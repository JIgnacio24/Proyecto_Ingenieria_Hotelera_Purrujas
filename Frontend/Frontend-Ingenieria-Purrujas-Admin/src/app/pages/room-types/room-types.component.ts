import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
<<<<<<< Updated upstream
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { RoomType, RoomTypesService } from '../../core/room-types.service';
=======
import { Component, computed, inject, signal } from '@angular/core';
import { RouterModule } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { RoomTypeDetail, RoomTypesService } from '../../core/room-types.service';
>>>>>>> Stashed changes

@Component({
  selector: 'app-room-types',
  standalone: true,
<<<<<<< Updated upstream
  imports: [CommonModule, FormsModule, RouterModule],
=======
  imports: [CommonModule, RouterModule],
>>>>>>> Stashed changes
  templateUrl: './room-types.component.html',
  styleUrl: './room-types.component.css'
})
export class RoomTypesComponent {
  private readonly roomTypesService = inject(RoomTypesService);

<<<<<<< Updated upstream
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly feedback = signal('');
  readonly feedbackTone = signal<'success' | 'error' | ''>('');
  readonly roomTypes = signal<RoomType[]>([]);
  readonly pendingDeletion = signal<RoomType | null>(null);

  roomTypeForm = this.createEmptyForm();
  editingRoomTypeId: number | null = null;
=======
  // ── Estado ──────────────────────────────────────────────────────────────
  readonly loading = signal(true);
  readonly error = signal('');
  readonly roomTypes = signal<RoomTypeDetail[]>([]);
  readonly selectedTypeId = signal<number | null>(null);

  /**
   * Tipo actualmente seleccionado; null si ninguno está elegido.
   */
  readonly selectedType = computed<RoomTypeDetail | null>(() => {
    const id = this.selectedTypeId();
    if (id === null) return null;
    return this.roomTypes().find(rt => rt.roomTypeId === id) ?? null;
  });
>>>>>>> Stashed changes

  constructor() {
    void this.loadRoomTypes();
  }

<<<<<<< Updated upstream
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
=======
  // ── Acciones ─────────────────────────────────────────────────────────────

  selectType(id: number): void {
    // Si se vuelve a pulsar el tipo ya seleccionado, se deselecciona.
    this.selectedTypeId.set(this.selectedTypeId() === id ? null : id);
  }

  // ── Formateo ──────────────────────────────────────────────────────────────

  formatUsd(value: number): string {
>>>>>>> Stashed changes
    return new Intl.NumberFormat('es-CR', {
      style: 'currency',
      currency: 'USD',
      maximumFractionDigits: 2
<<<<<<< Updated upstream
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
=======
    }).format(value);
  }

  /**
   * Indica si el tipo seleccionado carece de toda información descriptiva.
   * Se muestra el mensaje "Información no disponible" en ese caso.
   */
  hasNoDescriptiveInfo(type: RoomTypeDetail): boolean {
    return (
      !type.description.trim() &&
      type.capacity <= 0 &&
      type.images.length === 0
    );
  }

  // ── Carga de datos ────────────────────────────────────────────────────────

  private async loadRoomTypes(): Promise<void> {
    this.loading.set(true);
    this.error.set('');

    try {
      const types = await firstValueFrom(this.roomTypesService.getAll());
      this.roomTypes.set(types);
    } catch (err) {
      this.error.set(
        err instanceof HttpErrorResponse
          ? err.error?.message ?? err.message ?? 'No se pudieron cargar los tipos de habitación.'
          : 'Error inesperado al cargar los tipos de habitación.'
      );
    } finally {
      this.loading.set(false);
    }
>>>>>>> Stashed changes
  }
}
