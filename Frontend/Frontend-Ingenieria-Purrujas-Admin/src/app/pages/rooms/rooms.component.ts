import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { RoomTypesService, RoomTypeDetail } from '../../core/room-types.service';
import { RoomDetail, RoomPayload, RoomStatusOption, RoomsService } from '../../core/rooms.service';

@Component({
  selector: 'app-rooms',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './rooms.component.html',
  styleUrl: './rooms.component.css'
})
export class RoomsComponent {
  private readonly roomsService = inject(RoomsService);
  private readonly roomTypesService = inject(RoomTypesService);

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

  constructor() {
    void this.loadData();
  }

  selectRoom(roomId: number): void {
    this.selectedRoomId.set(this.selectedRoomId() === roomId ? null : roomId);
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
    if (roomTypeId === null) {
      return 'Sin tipo';
    }

    return this.roomTypes().find(roomType => roomType.roomTypeId === roomTypeId)?.name ?? 'Tipo no encontrado';
  }

  getStatus(roomStatusId: number | null): RoomStatusOption | null {
    if (roomStatusId === null) {
      return null;
    }

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
      if (seen.has(key)) {
        return false;
      }
      seen.add(key);
      return true;
    });
  }

  private clearFeedback(): void {
    this.feedback.set('');
    this.feedbackTone.set('');
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
}
