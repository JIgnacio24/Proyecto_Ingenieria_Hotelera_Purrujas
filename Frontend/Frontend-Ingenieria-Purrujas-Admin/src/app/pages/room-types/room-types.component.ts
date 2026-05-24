import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { RoomType, RoomTypesService } from '../../core/room-types.service';

@Component({
  selector: 'app-room-types',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './room-types.component.html',
  styleUrl: './room-types.component.css'
})
export class RoomTypesComponent {
  private readonly roomTypesService = inject(RoomTypesService);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly feedback = signal('');
  readonly feedbackTone = signal<'success' | 'error' | ''>('');
  readonly roomTypes = signal<RoomType[]>([]);
  readonly pendingDeletion = signal<RoomType | null>(null);

  roomTypeForm = this.createEmptyForm();
  editingRoomTypeId: number | null = null;

  constructor() {
    void this.loadRoomTypes();
  }

  async loadRoomTypes(): Promise<void> {
    this.loading.set(true);
    this.clearFeedback();

    try {
      const roomTypes = await firstValueFrom(this.roomTypesService.getAll());
      this.roomTypes.set(roomTypes);
    } catch (error) {
      this.feedbackTone.set('error');
      this.feedback.set(this.resolveError(error, 'No fue posible cargar los tipos de habitación.'));
    } finally {
      this.loading.set(false);
    }
  }

  async saveRoomType(): Promise<void> {
    this.saving.set(true);
    this.clearFeedback();

    try {
      const payload = {
        name: this.roomTypeForm.name.trim(),
        basePrice: Number(this.roomTypeForm.basePrice)
      };

      const savedRoomType = this.editingRoomTypeId
        ? await firstValueFrom(this.roomTypesService.update(this.editingRoomTypeId, payload))
        : await firstValueFrom(this.roomTypesService.create(payload));

      this.upsertRoomType(savedRoomType);
      this.resetForm();
      this.feedbackTone.set('success');
      this.feedback.set('El tipo de habitación se guardó correctamente.');
    } catch (error) {
      this.feedbackTone.set('error');
      this.feedback.set(this.resolveError(error, 'No fue posible guardar el tipo de habitación.'));
    } finally {
      this.saving.set(false);
    }
  }

  editRoomType(roomType: RoomType): void {
    this.editingRoomTypeId = roomType.roomTypeId;
    this.roomTypeForm = {
      name: roomType.name,
      basePrice: roomType.basePrice
    };
    this.clearFeedback();
  }

  resetForm(): void {
    this.editingRoomTypeId = null;
    this.roomTypeForm = this.createEmptyForm();
  }

  requestDelete(roomType: RoomType): void {
    this.pendingDeletion.set(roomType);
    this.clearFeedback();
  }

  cancelDeletion(): void {
    if (this.saving()) {
      return;
    }

    this.pendingDeletion.set(null);
  }

  async confirmDeletion(): Promise<void> {
    const roomType = this.pendingDeletion();
    if (!roomType) {
      return;
    }

    this.saving.set(true);
    this.clearFeedback();

    try {
      await firstValueFrom(this.roomTypesService.delete(roomType.roomTypeId));
      this.roomTypes.set(this.roomTypes().filter((item) => item.roomTypeId !== roomType.roomTypeId));

      if (this.editingRoomTypeId === roomType.roomTypeId) {
        this.resetForm();
      }

      this.pendingDeletion.set(null);
      this.feedbackTone.set('success');
      this.feedback.set('El tipo de habitación se eliminó correctamente.');
    } catch (error) {
      this.feedbackTone.set('error');
      this.feedback.set(this.resolveError(error, 'No fue posible eliminar el tipo de habitación.'));
    } finally {
      this.saving.set(false);
    }
  }

  formatUsd(value: number | null | undefined): string {
    return new Intl.NumberFormat('es-CR', {
      style: 'currency',
      currency: 'USD',
      maximumFractionDigits: 2
    }).format(value ?? 0);
  }

  private createEmptyForm(): { name: string; basePrice: number | null } {
    return {
      name: '',
      basePrice: null
    };
  }

  private upsertRoomType(roomType: RoomType): void {
    const currentRoomTypes = this.roomTypes();
    const nextRoomTypes = currentRoomTypes.some((item) => item.roomTypeId === roomType.roomTypeId)
      ? currentRoomTypes.map((item) => (item.roomTypeId === roomType.roomTypeId ? roomType : item))
      : [...currentRoomTypes, roomType];

    this.roomTypes.set(nextRoomTypes.sort((left, right) => left.name.localeCompare(right.name)));
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
