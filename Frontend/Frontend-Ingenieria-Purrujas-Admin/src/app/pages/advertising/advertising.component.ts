import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  AdminAdvertising,
  AdminAdvertisingService,
  AdvertisingPayload
} from '../../core/admin-advertising.service';

@Component({
  selector: 'app-advertising',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './advertising.component.html',
  styleUrl: './advertising.component.css'
})
export class AdvertisingComponent {
  private readonly adsService = inject(AdminAdvertisingService);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly feedback = signal('');
  readonly feedbackTone = signal<'success' | 'error' | ''>('');
  readonly ads = signal<AdminAdvertising[]>([]);
  readonly pendingDeletion = signal<AdminAdvertising | null>(null);

  form = this.emptyForm();
  editingId: number | null = null;

  constructor() {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.clearFeedback();
    try {
      const list = await firstValueFrom(this.adsService.getAll());
      this.ads.set(list);
    } catch (err) {
      this.feedbackTone.set('error');
      this.feedback.set(this.resolveError(err, 'No se pudo cargar la publicidad.'));
    } finally {
      this.loading.set(false);
    }
  }

  async save(): Promise<void> {
    this.saving.set(true);
    this.clearFeedback();
    try {
      const payload: AdvertisingPayload = {
        title:       this.form.title.trim(),
        description: this.form.description.trim(),
        imageUrl:    this.form.imageUrl.trim() || null,
        link:        this.form.link.trim(),
        isActive:    this.form.isActive
      };

      const saved = this.editingId
        ? await firstValueFrom(this.adsService.update(this.editingId, payload))
        : await firstValueFrom(this.adsService.create(payload));

      this.upsert(saved);
      this.reset();
      this.feedbackTone.set('success');
      this.feedback.set('Publicidad guardada correctamente.');
    } catch (err) {
      this.feedbackTone.set('error');
      this.feedback.set(this.resolveError(err, 'No se pudo guardar la publicidad.'));
    } finally {
      this.saving.set(false);
    }
  }

  startEdit(ad: AdminAdvertising): void {
    this.editingId = ad.advertisingId;
    this.form = {
      title:       ad.title,
      description: ad.description,
      imageUrl:    ad.imageUrl ?? '',
      link:        ad.link,
      isActive:    ad.isActive
    };
    this.clearFeedback();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  reset(): void {
    this.editingId = null;
    this.form = this.emptyForm();
  }

  requestDelete(ad: AdminAdvertising): void {
    this.pendingDeletion.set(ad);
    this.clearFeedback();
  }

  cancelDelete(): void {
    if (!this.saving()) this.pendingDeletion.set(null);
  }

  async confirmDelete(): Promise<void> {
    const ad = this.pendingDeletion();
    if (!ad) return;
    this.saving.set(true);
    this.clearFeedback();
    try {
      await firstValueFrom(this.adsService.delete(ad.advertisingId));
      this.ads.set(this.ads().filter(x => x.advertisingId !== ad.advertisingId));
      if (this.editingId === ad.advertisingId) this.reset();
      this.pendingDeletion.set(null);
      this.feedbackTone.set('success');
      this.feedback.set('Publicidad eliminada correctamente.');
    } catch (err) {
      this.feedbackTone.set('error');
      this.feedback.set(this.resolveError(err, 'No se pudo eliminar la publicidad.'));
    } finally {
      this.saving.set(false);
    }
  }

  formatDate(iso: string | null): string {
    if (!iso) return '—';
    const date = new Date(iso);
    if (Number.isNaN(date.getTime())) return iso;
    return new Intl.DateTimeFormat('es-CR', {
      dateStyle: 'medium',
      timeStyle: 'short'
    }).format(date);
  }

  truncate(text: string, max = 80): string {
    return text.length > max ? `${text.slice(0, max)}…` : text;
  }

  isValidUrl(url: string): boolean {
    try {
      new URL(url);
      return true;
    } catch {
      return false;
    }
  }

  isFormValid(): boolean {
    return (
      !!this.form.title.trim() &&
      !!this.form.description.trim() &&
      !!this.form.link.trim() &&
      this.isValidUrl(this.form.link.trim())
    );
  }

  private emptyForm() {
    return { title: '', description: '', imageUrl: '', link: '', isActive: true };
  }

  private upsert(ad: AdminAdvertising): void {
    const list = this.ads();
    const next = list.some(x => x.advertisingId === ad.advertisingId)
      ? list.map(x => x.advertisingId === ad.advertisingId ? ad : x)
      : [ad, ...list];
    this.ads.set(next);
  }

  private clearFeedback(): void {
    this.feedback.set('');
    this.feedbackTone.set('');
  }

  private resolveError(err: unknown, fallback: string): string {
    if (err instanceof HttpErrorResponse)
      return (err.error as { message?: string })?.message || err.message || fallback;
    if (err instanceof Error) return err.message;
    return fallback;
  }
}
