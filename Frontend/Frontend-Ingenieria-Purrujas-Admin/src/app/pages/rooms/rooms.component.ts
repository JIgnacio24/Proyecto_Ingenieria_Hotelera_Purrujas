import { CommonModule, DOCUMENT } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { RoomTypesService, RoomTypeDetail } from '../../core/room-types.service';
import { RoomDetail, RoomPayload, RoomStatusOption, RoomsService } from '../../core/rooms.service';
import {
  RoomAvailabilityReportResponse,
  RoomAvailabilitySearchResult,
  RoomAvailabilityService,
  RoomAvailabilitySummary,
  RoomTypeAvailabilityItem
} from '../../core/room-availability.service';

@Component({
  selector: 'app-rooms',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './rooms.component.html',
  styleUrl: './rooms.component.css'
})
export class RoomsComponent {
  private readonly document = inject(DOCUMENT);
  private readonly authService = inject(AuthService);
  private readonly roomsService = inject(RoomsService);
  private readonly roomTypesService = inject(RoomTypesService);
  private readonly roomAvailabilityService = inject(RoomAvailabilityService);

  // ── Rooms ─────────────────────────────────────────────────────────────────
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly feedback = signal('');
  readonly feedbackTone = signal<'success' | 'error' | ''>('');
  readonly rooms = signal<RoomDetail[]>([]);
  readonly roomTypes = signal<RoomTypeDetail[]>([]);
  readonly roomStatuses = signal<RoomStatusOption[]>([]);
  readonly pendingDeletion = signal<RoomDetail | null>(null);
  readonly selectedRoomId = signal<number | null>(null);

  readonly selectedRoom = computed<RoomDetail | null>(() => {
    const roomId = this.selectedRoomId();
    if (roomId === null) return null;
    return this.rooms().find(room => room.roomId === roomId) ?? null;
  });

  roomForm = this.createEmptyForm();
  editingRoomId: number | null = null;

  // ── Availability ──────────────────────────────────────────────────────────
  readonly availabilityLoading = signal(true);
  readonly availabilitySearching = signal(false);
  readonly availabilityReportBusy = signal(false);
  readonly availabilityFeedback = signal('');
  readonly availabilityFeedbackTone = signal<'success' | 'error' | ''>('');
  readonly roomAvailabilitySummary = signal<RoomAvailabilitySummary | null>(null);
  readonly availabilitySearchResult = signal<RoomAvailabilitySearchResult | null>(null);

  availabilityStartDate = this.todayInputValue();
  availabilityEndDate = this.addDaysInputValue(1);
  availabilityRoomTypeId: number | null = null;

  constructor() {
    void this.loadData();
    void this.loadRoomAvailabilityToday();
  }

  // ── Rooms methods ─────────────────────────────────────────────────────────

  selectRoom(roomId: number): void {
    this.selectedRoomId.set(this.selectedRoomId() === roomId ? null : roomId);
  }

  onRoomSelect(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.selectedRoomId.set(value ? +value : null);
  }

  async loadData(): Promise<void> {
    this.loading.set(true);
    this.clearFeedback();

    try {
      const [rooms, roomTypes, roomStatuses] = await Promise.all([
        firstValueFrom(this.roomsService.getAll()),
        firstValueFrom(this.roomTypesService.getAll()),
        firstValueFrom(this.roomsService.getStatuses())
      ]);

      this.rooms.set(rooms);
      this.roomTypes.set(this.dedupeRoomTypes(roomTypes));
      this.roomStatuses.set(roomStatuses);

      if (this.selectedRoomId() === null && rooms.length > 0) {
        this.selectedRoomId.set(rooms[0].roomId);
      }
    } catch (error) {
      this.feedbackTone.set('error');
      this.feedback.set(this.resolveError(error, 'No fue posible cargar las habitaciones.'));
    } finally {
      this.loading.set(false);
    }
  }

  async saveRoom(): Promise<void> {
    if (this.roomForm.roomTypeId === null || this.roomForm.roomStatusId === null) {
      return;
    }

    this.saving.set(true);
    this.clearFeedback();

    try {
      const payload: RoomPayload = {
        roomNumber: this.roomForm.roomNumber.trim(),
        roomTypeId: this.roomForm.roomTypeId,
        roomStatusId: this.roomForm.roomStatusId
      };

      const saved = this.editingRoomId
        ? await firstValueFrom(this.roomsService.update(this.editingRoomId, payload))
        : await firstValueFrom(this.roomsService.create(payload));

      this.upsertRoom(saved);
      this.selectedRoomId.set(saved.roomId);
      this.resetForm();
      this.feedbackTone.set('success');
      this.feedback.set('La habitación se guardó correctamente.');
    } catch (error) {
      this.feedbackTone.set('error');
      this.feedback.set(this.resolveError(error, 'No fue posible guardar la habitación.'));
    } finally {
      this.saving.set(false);
    }
  }

  editRoom(room: RoomDetail): void {
    this.editingRoomId = room.roomId;
    this.roomForm = {
      roomNumber: room.roomNumber,
      roomTypeId: room.roomTypeId,
      roomStatusId: room.roomStatusId
    };
    this.clearFeedback();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  resetForm(): void {
    this.editingRoomId = null;
    this.roomForm = this.createEmptyForm();
  }

  requestDelete(room: RoomDetail): void {
    this.pendingDeletion.set(room);
    this.clearFeedback();
  }

  cancelDeletion(): void {
    if (this.saving()) return;
    this.pendingDeletion.set(null);
  }

  async confirmDeletion(): Promise<void> {
    const room = this.pendingDeletion();
    if (!room) return;

    this.saving.set(true);
    this.clearFeedback();
    this.pendingDeletion.set(null);

    try {
      await firstValueFrom(this.roomsService.delete(room.roomId));
      this.removeRoomLocally(room.roomId);
      if (this.editingRoomId === room.roomId) {
        this.resetForm();
      }
      if (this.selectedRoomId() === room.roomId) {
        this.selectedRoomId.set(null);
      }

      await this.loadData();
      this.feedbackTone.set('success');
      this.feedback.set('La habitación se eliminó correctamente.');
    } catch (error) {
      this.feedbackTone.set('error');
      this.feedback.set(this.resolveError(error, 'No fue posible eliminar la habitación.'));
    } finally {
      this.saving.set(false);
    }
  }

  getRoomTypeName(roomTypeId: number | null): string {
    if (roomTypeId === null) return 'Sin tipo';
    return this.roomTypes().find(roomType => roomType.roomTypeId === roomTypeId)?.name ?? 'Tipo no encontrado';
  }

  getStatus(roomStatusId: number | null): RoomStatusOption | null {
    if (roomStatusId === null) return null;
    return this.roomStatuses().find(status => status.roomStatusId === roomStatusId) ?? null;
  }

  getStatusLabel(roomStatusId: number | null): string {
    return this.getStatus(roomStatusId)?.name ?? 'Sin estado';
  }

  getStatusTone(roomStatusId: number | null): 'active' | 'inactive' {
    return this.getStatus(roomStatusId)?.isAvailableForBooking ? 'active' : 'inactive';
  }

  hasNoDetails(room: RoomDetail): boolean {
    return !room.roomNumber.trim() || !room.roomTypeName.trim() || !room.roomStatusName.trim();
  }

  // ── Availability methods ──────────────────────────────────────────────────

  async loadRoomAvailabilityToday(): Promise<void> {
    this.availabilityLoading.set(true);
    this.clearAvailabilityFeedback();

    try {
      const summary = await firstValueFrom(this.roomAvailabilityService.getToday());
      this.roomAvailabilitySummary.set(summary);
    } catch (error) {
      this.availabilityFeedbackTone.set('error');
      this.availabilityFeedback.set(
        this.resolveError(error, 'No fue posible cargar el estado de habitaciones de hoy.')
      );
      if (error instanceof HttpErrorResponse && (error.status === 401 || error.status === 403)) {
        this.authService.logout();
      }
    } finally {
      this.availabilityLoading.set(false);
    }
  }

  async searchRoomAvailability(): Promise<void> {
    this.availabilitySearching.set(true);
    this.clearAvailabilityFeedback();

    try {
      const result = await firstValueFrom(
        this.roomAvailabilityService.search(
          this.availabilityStartDate,
          this.availabilityEndDate,
          this.availabilityRoomTypeId
        )
      );
      this.availabilitySearchResult.set(result);
      this.availabilityFeedbackTone.set('success');
      this.availabilityFeedback.set('Consulta de disponibilidad actualizada.');
    } catch (error) {
      this.availabilityFeedbackTone.set('error');
      this.availabilityFeedback.set(
        this.resolveError(error, 'No fue posible consultar disponibilidad para esas fechas.')
      );
      if (error instanceof HttpErrorResponse && (error.status === 401 || error.status === 403)) {
        this.authService.logout();
      }
    } finally {
      this.availabilitySearching.set(false);
    }
  }

  async openRoomAvailabilityReportPreview(): Promise<void> {
    await this.handleRoomAvailabilityReport('preview');
  }

  async downloadRoomAvailabilityReport(): Promise<void> {
    await this.handleRoomAvailabilityReport('download');
  }

  formatAvailabilityDate(value: string | null | undefined): string {
    if (!value) return 'Sin fecha';
    const date = new Date(`${value}T00:00:00`);
    if (Number.isNaN(date.getTime())) return value;
    return new Intl.DateTimeFormat('es-CR', { dateStyle: 'full' }).format(date);
  }

  statusClass(statusName: string): string {
    const normalized = statusName.trim().toLowerCase();
    if (normalized === 'disponible') return 'status-available';
    if (normalized === 'ocupada') return 'status-occupied';
    return 'status-blocked';
  }

  formatUsd(value: number | null | undefined): string {
    return new Intl.NumberFormat('es-CR', {
      style: 'currency',
      currency: 'USD',
      maximumFractionDigits: 2
    }).format(value ?? 0);
  }

  availabilityTypeItems(result: RoomAvailabilitySearchResult): RoomTypeAvailabilityItem[] {
    return result.roomTypeAvailability.filter((item) => {
      if (result.roomTypeId && item.roomTypeId === result.roomTypeId) return false;
      return item.availableRooms > 0;
    });
  }

  filteredSuggestedDateRanges(
    result: RoomAvailabilitySearchResult
  ): RoomAvailabilitySearchResult['suggestedDateRanges'] {
    const today = this.todayInputValue();
    return result.suggestedDateRanges.filter(
      (s) => s.startDate >= today && s.endDate > today
    );
  }

  availabilityEmptyMessage(result: RoomAvailabilitySearchResult): string {
    const hasOtherAvailability = this.availabilityTypeItems(result).some((item) => item.availableRooms > 0);
    if (result.roomTypeId && hasOtherAvailability) {
      return 'No hay habitaciones disponibles de este tipo, pero existen habitaciones disponibles en otras categorías.';
    }
    if (result.roomTypeId) {
      return `No hay habitaciones disponibles de ${result.roomTypeName} para el rango seleccionado.`;
    }
    return 'No hay habitaciones disponibles para esta consulta.';
  }

  availabilityDateSuggestionLabel(suggestion: RoomAvailabilitySearchResult['suggestedDateRanges'][number]): string {
    return `${this.formatAvailabilityDate(suggestion.startDate)} - ${this.formatAvailabilityDate(suggestion.endDate)}`;
  }

  hasUnavailableTypeSummary(result: RoomAvailabilitySearchResult): boolean {
    return this.availabilityTypeItems(result).length > 0;
  }

  // ── Private helpers ───────────────────────────────────────────────────────

  private createEmptyForm(): { roomNumber: string; roomTypeId: number | null; roomStatusId: number | null } {
    return { roomNumber: '', roomTypeId: null, roomStatusId: null };
  }

  private upsertRoom(room: RoomDetail): void {
    const current = this.rooms();
    const next = current.some(item => item.roomId === room.roomId)
      ? current.map(item => item.roomId === room.roomId ? room : item)
      : [...current, room];
    this.rooms.set(next.sort((a, b) => a.roomNumber.localeCompare(b.roomNumber, 'es', { numeric: true })));
  }

  private removeRoomLocally(roomId: number): void {
    this.rooms.set(this.rooms().filter(item => item.roomId !== roomId));
  }

  private dedupeRoomTypes(roomTypes: RoomTypeDetail[]): RoomTypeDetail[] {
    const seen = new Set<string>();
    return roomTypes.filter(roomType => {
      const key = String(roomType.roomTypeId);
      if (seen.has(key)) return false;
      seen.add(key);
      return true;
    });
  }

  private clearFeedback(): void {
    this.feedback.set('');
    this.feedbackTone.set('');
  }

  private clearAvailabilityFeedback(): void {
    this.availabilityFeedback.set('');
    this.availabilityFeedbackTone.set('');
  }

  private resolveError(error: unknown, fallbackMessage: string): string {
    if (error instanceof HttpErrorResponse) {
      return error.error?.message || error.message || fallbackMessage;
    }
    if (error instanceof Error && error.message) {
      return error.message;
    }
    return fallbackMessage;
  }

  private async handleRoomAvailabilityReport(mode: 'preview' | 'download'): Promise<void> {
    this.availabilityReportBusy.set(true);
    this.clearAvailabilityFeedback();

    try {
      const report = await firstValueFrom(this.roomAvailabilityService.getTodayReport());
      const file = new File([report.blob], report.fileName, { type: 'application/pdf' });

      if (mode === 'preview') {
        const previewOpened = this.openPdfPreview(file, report);
        this.availabilityFeedbackTone.set('success');
        this.availabilityFeedback.set(
          previewOpened
            ? 'El reporte PDF se generó con el estado actual de las habitaciones y se abrió en una nueva pestaña.'
            : 'El navegador bloqueó la vista previa. El reporte PDF se descargó automáticamente.'
        );
      } else {
        this.downloadPdfFile(file, report.fileName);
        this.availabilityFeedbackTone.set('success');
        this.availabilityFeedback.set('El reporte PDF se descargó correctamente.');
      }
    } catch (error) {
      this.availabilityFeedbackTone.set('error');
      this.availabilityFeedback.set(
        this.resolveError(error, 'No fue posible generar el reporte PDF del estado de habitaciones.')
      );
      if (error instanceof HttpErrorResponse && (error.status === 401 || error.status === 403)) {
        this.authService.logout();
      }
    } finally {
      this.availabilityReportBusy.set(false);
    }
  }

  private openPdfPreview(file: File, report: RoomAvailabilityReportResponse): boolean {
    if (typeof window === 'undefined' || typeof URL === 'undefined') {
      this.downloadPdfFile(file, report.fileName);
      return false;
    }
    const objectUrl = URL.createObjectURL(file);
    const previewWindow = window.open(objectUrl, '_blank', 'noopener');
    if (!previewWindow) {
      this.downloadPdfFile(file, report.fileName);
      return false;
    }
    window.setTimeout(() => URL.revokeObjectURL(objectUrl), 60_000);
    return true;
  }

  private downloadPdfFile(file: File, fileName: string): void {
    if (typeof URL === 'undefined') return;
    const objectUrl = URL.createObjectURL(file);
    const anchor = this.document.createElement('a');
    anchor.href = objectUrl;
    anchor.download = fileName;
    anchor.rel = 'noopener';
    anchor.click();
    window.setTimeout(() => URL.revokeObjectURL(objectUrl), 30_000);
  }

  todayInputValue(): string {
    return this.dateInputValue(new Date());
  }

  minEndDate(): string {
    const start = new Date(this.availabilityStartDate + 'T00:00:00');
    start.setDate(start.getDate() + 1);
    return this.dateInputValue(start);
  }

  private addDaysInputValue(days: number): string {
    const date = new Date();
    date.setDate(date.getDate() + days);
    return this.dateInputValue(date);
  }

  private dateInputValue(date: Date): string {
    return [
      date.getFullYear(),
      `${date.getMonth() + 1}`.padStart(2, '0'),
      `${date.getDate()}`.padStart(2, '0')
    ].join('-');
  }
}
