import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AdminPromotion, AdminPromotionsService, PromotionPayload } from '../../core/admin-promotions.service';
import { RoomTypeDetail, RoomTypesService } from '../../core/room-types.service';

@Component({
  selector: 'app-promotions',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './promotions.component.html',
  styleUrl: './promotions.component.css'
})
export class PromotionsComponent {
  private readonly promoService = inject(AdminPromotionsService);
  private readonly roomTypesService = inject(RoomTypesService);

  // ── Estado ──────────────────────────────────────────────────────────────
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly feedback = signal('');
  readonly feedbackTone = signal<'success' | 'error' | ''>('');
  readonly promotions = signal<AdminPromotion[]>([]);
  readonly roomTypes = signal<RoomTypeDetail[]>([]);
  readonly pendingDeletion = signal<AdminPromotion | null>(null);

  form = this.emptyForm();
  editingId: number | null = null;

  constructor() {
    void this.load();
  }

  // ── Carga ─────────────────────────────────────────────────────────────────

  async load(): Promise<void> {
    this.loading.set(true);
    this.clearFeedback();
    try {
      const [promos, types] = await Promise.all([
        firstValueFrom(this.promoService.getAll()),
        firstValueFrom(this.roomTypesService.getAll())
      ]);
      this.promotions.set(promos);
      this.roomTypes.set(types.filter(t => t.isActive));
    } catch (err) {
      this.feedbackTone.set('error');
      this.feedback.set(this.resolveError(err, 'No se pudieron cargar las ofertas.'));
    } finally {
      this.loading.set(false);
    }
  }

  // ── Guardar ───────────────────────────────────────────────────────────────

  async save(): Promise<void> {
    this.saving.set(true);
    this.clearFeedback();
    try {
      const payload: PromotionPayload = {
        name: this.form.name.trim(),
        discount: Number(this.form.discount),
        startDate: this.form.startDate,
        endDate: this.form.endDate,
        roomTypeId: Number(this.form.roomTypeId)
      };

      const saved = this.editingId
        ? await firstValueFrom(this.promoService.update(this.editingId, payload))
        : await firstValueFrom(this.promoService.create(payload));

      this.upsert(saved);
      this.reset();
      this.feedbackTone.set('success');
      this.feedback.set('Oferta especial guardada correctamente.');
    } catch (err) {
      this.feedbackTone.set('error');
      this.feedback.set(this.resolveError(err, 'No se pudo guardar la oferta.'));
    } finally {
      this.saving.set(false);
    }
  }

  // ── Editar ────────────────────────────────────────────────────────────────

  startEdit(p: AdminPromotion): void {
    this.editingId = p.promotionId;
    this.form = {
      name: p.name,
      discount: p.discount,
      startDate: p.startDate,
      endDate: p.endDate,
      roomTypeId: p.roomTypeId
    };
    this.clearFeedback();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  reset(): void {
    this.editingId = null;
    this.form = this.emptyForm();
  }

  // ── Eliminar ──────────────────────────────────────────────────────────────

  requestDelete(p: AdminPromotion): void {
    this.pendingDeletion.set(p);
    this.clearFeedback();
  }

  cancelDelete(): void {
    if (!this.saving()) this.pendingDeletion.set(null);
  }

  async confirmDelete(): Promise<void> {
    const p = this.pendingDeletion();
    if (!p) return;
    this.saving.set(true);
    this.clearFeedback();
    try {
      await firstValueFrom(this.promoService.delete(p.promotionId));
      this.promotions.set(this.promotions().filter(x => x.promotionId !== p.promotionId));
      if (this.editingId === p.promotionId) this.reset();
      this.pendingDeletion.set(null);
      this.feedbackTone.set('success');
      this.feedback.set('Oferta eliminada correctamente.');
    } catch (err) {
      this.feedbackTone.set('error');
      this.feedback.set(this.resolveError(err, 'No se pudo eliminar la oferta.'));
    } finally {
      this.saving.set(false);
    }
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  formatDate(iso: string): string {
    if (!iso) return '—';
    const [y, m, d] = iso.split('T')[0].split('-');
    return `${d}/${m}/${y}`;
  }

  isFormValid(): boolean {
    return (
      !!this.form.name.trim() &&
      this.form.discount > 0 &&
      this.form.discount <= 100 &&
      !!this.form.startDate &&
      !!this.form.endDate &&
      this.form.startDate < this.form.endDate &&
      this.form.roomTypeId > 0
    );
  }

  private emptyForm() {
    return {
      name: '',
      discount: 0 as number,
      startDate: '',
      endDate: '',
      roomTypeId: 0 as number
    };
  }

  private upsert(p: AdminPromotion): void {
    const list = this.promotions();
    const next = list.some(x => x.promotionId === p.promotionId)
      ? list.map(x => x.promotionId === p.promotionId ? p : x)
      : [p, ...list];
    this.promotions.set(next);
  }

  private clearFeedback(): void {
    this.feedback.set('');
    this.feedbackTone.set('');
  }

  private resolveError(err: unknown, fallback: string): string {
    if (err instanceof HttpErrorResponse)
      return err.error?.message || err.message || fallback;
    if (err instanceof Error) return err.message;
    return fallback;
  }
}
