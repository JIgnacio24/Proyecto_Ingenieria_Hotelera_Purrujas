import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AdminReservationResponse, ReservationService, RoomAvailabilityItem } from '../../core/reservation.service';

type StatusFilter = 'todas' | 'Pendiente' | 'Confirmada' | 'Finalizada' | 'Cancelada' | 'Eliminadas';

@Component({
  selector: 'app-reservations',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './reservations.component.html',
  styleUrl: './reservations.component.css'
})
export class ReservationsComponent {
  private readonly reservationService = inject(ReservationService);

  // List state
  readonly loading = signal(true);
  readonly error = signal('');
  readonly reservations = signal<AdminReservationResponse[]>([]);
  readonly deletedReservations = signal<AdminReservationResponse[]>([]);
  readonly statusFilter = signal<StatusFilter>('todas');
  readonly updatingId = signal<number | null>(null);
  readonly updateFeedback = signal('');
  readonly updateFeedbackTone = signal<'success' | 'error' | ''>('');


  // Delete state
  readonly pendingDeleteReservation = signal<AdminReservationResponse | null>(null);

  // Edit state
  readonly editingReservation = signal<AdminReservationResponse | null>(null);
  readonly editStartDate = signal('');
  readonly editEndDate = signal('');
  readonly editRoomId = signal<number | null>(null);
  readonly availableRooms = signal<RoomAvailabilityItem[]>([]);
  readonly loadingRooms = signal(false);
  readonly savingEdit = signal(false);
  readonly editValidationError = signal('');
  readonly roomsLoaded = signal(false);

  readonly statusOptions: StatusFilter[] = ['todas', 'Pendiente', 'Confirmada', 'Finalizada', 'Cancelada', 'Eliminadas'];
  readonly changeableStatuses = ['Pendiente', 'Confirmada', 'Cancelada'];

  constructor() {
    void this.loadReservations();
  }

  // ── Computed ──────────────────────────────────────────────────────────────

  get filteredReservations(): AdminReservationResponse[] {
    const filter = this.statusFilter();
    if (filter === 'Eliminadas') return this.deletedReservations();
    const active = this.reservations();
    if (filter === 'todas') return active;
    return active.filter(r => r.status === filter);
  }

  get todayStr(): string {
    const d = new Date();
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }

  onRoomChange(value: string): void {
    this.editRoomId.set(Number(value));
  }

  get selectedRoom(): RoomAvailabilityItem | undefined {
    const id = this.editRoomId();
    if (id === null) return undefined;
    return this.availableRooms().find(r => r.roomId === id);
  }

  // ── Filter / Count ────────────────────────────────────────────────────────

  setFilter(status: StatusFilter): void {
    this.statusFilter.set(status);
  }

  countByStatus(status: StatusFilter): number {
    if (status === 'Eliminadas') return this.deletedReservations().length;
    const active = this.reservations();
    if (status === 'todas') return active.length;
    return active.filter(r => r.status === status).length;
  }

  // ── Delete ────────────────────────────────────────────────────────────────

  private parseLocalDate(dateStr: string): Date {
    const [y, m, d] = dateStr.substring(0, 10).split('-').map(Number);
    return new Date(y, m - 1, d);
  }

  canEdit(r: AdminReservationResponse): boolean {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return this.parseLocalDate(r.endDate) >= today;
  }

  canDelete(r: AdminReservationResponse): boolean {
    if (r.status !== 'Cancelada') return false;
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return this.parseLocalDate(r.endDate) >= today;
  }

  requestDelete(r: AdminReservationResponse): void {
    this.pendingDeleteReservation.set(r);
  }

  cancelDelete(): void {
    this.pendingDeleteReservation.set(null);
  }

  async confirmDelete(): Promise<void> {
    const r = this.pendingDeleteReservation();
    if (!r) return;

    this.pendingDeleteReservation.set(null);
    this.clearFeedback();

    try {
      await firstValueFrom(this.reservationService.softDelete(r.reservationId));
      this.reservations.set(this.reservations().filter(x => x.reservationId !== r.reservationId));
      this.deletedReservations.set([r, ...this.deletedReservations()]);
      this.updateFeedback.set(`Reserva #${r.reservationId} eliminada.`);
      this.updateFeedbackTone.set('success');
    } catch (err) {
      this.updateFeedbackTone.set('error');
      this.updateFeedback.set(
        err instanceof HttpErrorResponse
          ? `Error ${err.status}: ${err.error?.message || err.statusText || 'No se pudo eliminar la reserva.'}`
          : 'Error inesperado al eliminar.'
      );
    }
  }

  // ── Edit ──────────────────────────────────────────────────────────────────

  openEdit(r: AdminReservationResponse): void {
    this.editingReservation.set(r);
    this.editStartDate.set(r.startDate.substring(0, 10));
    this.editEndDate.set(r.endDate.substring(0, 10));
    this.editRoomId.set(null);
    this.availableRooms.set([]);
    this.roomsLoaded.set(false);
    this.loadingRooms.set(false);
    this.savingEdit.set(false);
    this.editValidationError.set('');
    void this.loadAvailableRooms();
  }

  closeEdit(): void {
    this.editingReservation.set(null);
  }

  onStartDateChange(value: string): void {
    this.editStartDate.set(value);
    this.onDatesChange();
  }

  onEndDateChange(value: string): void {
    this.editEndDate.set(value);
    this.onDatesChange();
  }

onDatesChange(): void {
    const previousRoomId = this.editRoomId();
    this.editRoomId.set(null);
    this.availableRooms.set([]);
    this.roomsLoaded.set(false);
    this.editValidationError.set('');

    const start = this.editStartDate();
    const end = this.editEndDate();
    if (!start || !end) return;

    if (new Date(end) <= new Date(start)) {
      this.editValidationError.set('La fecha de salida debe ser posterior a la fecha de entrada.');
      return;
    }

    void this.loadAvailableRooms(previousRoomId);
  }

  canSaveEdit(): boolean {
    return !!this.editStartDate()
      && !!this.editEndDate()
      && this.editRoomId() !== null
      && !this.editValidationError()
      && !this.loadingRooms()
      && !this.savingEdit();
  }

  async saveEdit(): Promise<void> {
    const r = this.editingReservation();
    const roomId = this.editRoomId();
    const startDate = this.editStartDate();
    const endDate = this.editEndDate();
    if (!r || !roomId || !startDate || !endDate) return;

    this.savingEdit.set(true);
    this.editValidationError.set('');
    this.clearFeedback();

    try {
      const updated = await firstValueFrom(
        this.reservationService.updateReservation(r.reservationId, { roomId, startDate, endDate })
      );
      this.reservations.set(
        this.reservations().map(x => x.reservationId === r.reservationId ? updated : x)
      );

      this.editingReservation.set(null);
      this.updateFeedback.set(`Reserva #${r.reservationId} actualizada correctamente.`);
      this.updateFeedbackTone.set('success');
    } catch (err) {
      this.editValidationError.set(
        err instanceof HttpErrorResponse
          ? err.error?.message || `Error ${err.status}: No se pudo actualizar la reserva.`
          : 'Error inesperado al actualizar.'
      );
    } finally {
      this.savingEdit.set(false);
    }
  }

  private async loadAvailableRooms(preferredRoomId?: number | null): Promise<void> {
    const start = this.editStartDate();
    const end = this.editEndDate();
    if (!start || !end) return;

    const current = this.editingReservation();
    const targetId = preferredRoomId ?? current?.roomId ?? null;

    this.loadingRooms.set(true);
    try {
      const result = await firstValueFrom(
        this.reservationService.searchAvailableRooms(start, end, current?.reservationId)
      );

      const rooms = result.rooms;
      const found = targetId !== null ? rooms.find(rm => rm.roomId === targetId) : undefined;
      this.editRoomId.set(found?.roomId ?? rooms[0]?.roomId ?? null);

      this.availableRooms.set(rooms);
      this.roomsLoaded.set(true);
    } catch {
      this.editValidationError.set('No se pudieron cargar las habitaciones disponibles.');
    } finally {
      this.loadingRooms.set(false);
    }
  }

  // ── Status update ─────────────────────────────────────────────────────────

  async updateStatus(reservation: AdminReservationResponse, newStatus: string): Promise<void> {
    if (!newStatus || newStatus === reservation.status) return;

    this.updatingId.set(reservation.reservationId);
    this.clearFeedback();

    try {
      await firstValueFrom(this.reservationService.updateStatus(reservation.reservationId, newStatus));
      const updated = this.reservations().map(r =>
        r.reservationId === reservation.reservationId ? { ...r, status: newStatus } : r
      );
      this.reservations.set(updated);
      this.updateFeedback.set(`Reserva #${reservation.reservationId} actualizada a "${newStatus}".`);
      this.updateFeedbackTone.set('success');
    } catch (err) {
      this.updateFeedbackTone.set('error');
      this.updateFeedback.set(
        err instanceof HttpErrorResponse
          ? err.error?.message || 'No se pudo actualizar el estado.'
          : 'Error inesperado al actualizar.'
      );
    } finally {
      this.updatingId.set(null);
    }
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  formatDate(dateStr: string): string {
    if (!dateStr) return '—';
    const [year, month, day] = dateStr.substring(0, 10).split('-').map(Number);
    const d = new Date(year, month - 1, day);
    return new Intl.DateTimeFormat('es-CR', { day: 'numeric', month: 'short', year: 'numeric' }).format(d);
  }

  statusClass(status: string): string {
    switch (status) {
      case 'Pendiente':  return 'status-badge status-badge--pending';
      case 'Confirmada': return 'status-badge status-badge--confirmed';
      case 'Finalizada': return 'status-badge status-badge--done';
      case 'Cancelada':  return 'status-badge status-badge--cancelled';
      default:           return 'status-badge';
    }
  }

  private async loadReservations(): Promise<void> {
    this.loading.set(true);
    this.error.set('');
    try {
      const [active, deleted] = await Promise.all([
        firstValueFrom(this.reservationService.getAll()),
        firstValueFrom(this.reservationService.getDeleted()).catch(() => [] as AdminReservationResponse[])
      ]);
      this.reservations.set(active);
      this.deletedReservations.set(deleted);
    } catch (err) {
      this.error.set(
        err instanceof HttpErrorResponse
          ? err.error?.message || 'No se pudieron cargar las reservaciones.'
          : 'Error al cargar reservaciones.'
      );
    } finally {
      this.loading.set(false);
    }
  }

  private clearFeedback(): void {
    this.updateFeedback.set('');
    this.updateFeedbackTone.set('');
  }
}
