import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { RoomTypeDetail, RoomTypePayload, RoomTypesService } from '../../core/room-types.service';

@Component({
  selector: 'app-room-types',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './room-types.component.html',
  styleUrl: './room-types.component.css'
})
export class RoomTypesComponent {
  private readonly roomTypesService = inject(RoomTypesService);

  // ── Estado ──────────────────────────────────────────────────────────────
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly feedback = signal('');
  readonly feedbackTone = signal<'success' | 'error' | ''>('');
  readonly roomTypes = signal<RoomTypeDetail[]>([]);
  readonly pendingDeletion = signal<RoomTypeDetail | null>(null);
  readonly selectedTypeId = signal<number | null>(null);

  /** Tipo actualmente seleccionado; null si ninguno está elegido. */
  readonly selectedType = computed<RoomTypeDetail | null>(() => {
    const id = this.selectedTypeId();
    if (id === null) return null;
    return this.roomTypes().find(rt => rt.roomTypeId === id) ?? null;
  });

  roomTypeForm = this.createEmptyForm();
  editingRoomTypeId: number | null = null;

  constructor() {
    void this.loadRoomTypes();
  }

  // ── Selección de tipo ─────────────────────────────────────────────────────

  selectType(id: number): void {
    this.selectedTypeId.set(this.selectedTypeId() === id ? null : id);
  }

  // ── Carga de datos ────────────────────────────────────────────────────────

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

  // ── Guardar (crear o editar) ──────────────────────────────────────────────

  async saveRoomType(): Promise<void> {
    this.saving.set(true);
    this.clearFeedback();

    try {
      const payload: RoomTypePayload = {
        name: this.roomTypeForm.name.trim(),
        basePrice: Number(this.roomTypeForm.basePrice)
      };

      const saved = this.editingRoomTypeId
        ? await firstValueFrom(this.roomTypesService.update(this.editingRoomTypeId, payload))
        : await firstValueFrom(this.roomTypesService.create(payload));

      this.upsertRoomType(saved);
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

  // ── Editar ────────────────────────────────────────────────────────────────

  editRoomType(roomType: RoomTypeDetail): void {
    this.editingRoomTypeId = roomType.roomTypeId;
    this.roomTypeForm = {
      name: roomType.name,
      basePrice: roomType.basePrice
    };
    this.clearFeedback();
    // Llevar el foco al formulario de edición
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  resetForm(): void {
    this.editingRoomTypeId = null;
    this.roomTypeForm = this.createEmptyForm();
  }

  // ── Eliminar ──────────────────────────────────────────────────────────────

  requestDelete(roomType: RoomTypeDetail): void {
    this.pendingDeletion.set(roomType);
    this.clearFeedback();
  }

  cancelDeletion(): void {
    if (this.saving()) return;
    this.pendingDeletion.set(null);
  }

  async confirmDeletion(): Promise<void> {
    const roomType = this.pendingDeletion();
    if (!roomType) return;

    this.saving.set(true);
    this.clearFeedback();

    try {
      await firstValueFrom(this.roomTypesService.delete(roomType.roomTypeId));
      this.roomTypes.set(this.roomTypes().filter(item => item.roomTypeId !== roomType.roomTypeId));

      if (this.editingRoomTypeId === roomType.roomTypeId) {
        this.resetForm();
      }
      if (this.selectedTypeId() === roomType.roomTypeId) {
        this.selectedTypeId.set(null);
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

  // ── Formateo / helpers ────────────────────────────────────────────────────

  formatUsd(value: number | null | undefined): string {
    return new Intl.NumberFormat('es-CR', {
      style: 'currency',
      currency: 'USD',
      maximumFractionDigits: 2
    }).format(value ?? 0);
  }

  hasNoDescriptiveInfo(type: RoomTypeDetail): boolean {
    return (
      !type.description.trim() &&
      type.capacity <= 0 &&
      type.images.length === 0
    );
  }

  // ── Privados ──────────────────────────────────────────────────────────────

  private createEmptyForm(): { name: string; basePrice: number | null } {
    return { name: '', basePrice: null };
  }

  private upsertRoomType(roomType: RoomTypeDetail): void {
    const current = this.roomTypes();
    const next = current.some(item => item.roomTypeId === roomType.roomTypeId)
      ? current.map(item => item.roomTypeId === roomType.roomTypeId ? roomType : item)
      : [...current, roomType];
    this.roomTypes.set(next.sort((a, b) => a.name.localeCompare(b.name)));
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
