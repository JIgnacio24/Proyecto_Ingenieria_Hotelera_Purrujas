import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { Season, SeasonPayload, SeasonsService } from '../../core/seasons.service';

@Component({
  selector: 'app-seasons',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './seasons.component.html',
  styleUrl: './seasons.component.css'
})
export class SeasonsComponent {
  private readonly seasonsService = inject(SeasonsService);

  // ── Estado ──────────────────────────────────────────────────────────────
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly feedback = signal('');
  readonly feedbackTone = signal<'success' | 'error' | ''>('');
  readonly seasons = signal<Season[]>([]);
  readonly pendingDeletion = signal<Season | null>(null);

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
      const data = await firstValueFrom(this.seasonsService.getAll());
      this.seasons.set(data);
    } catch (err) {
      this.feedbackTone.set('error');
      this.feedback.set(this.resolveError(err, 'No se pudieron cargar las temporadas.'));
    } finally {
      this.loading.set(false);
    }
  }

  // ── Guardar ───────────────────────────────────────────────────────────────

  async save(): Promise<void> {
    this.saving.set(true);
    this.clearFeedback();
    try {
      const payload: SeasonPayload = {
        name: this.form.name.trim(),
        percentageChange: Number(this.form.percentageChange),
        startDate: this.form.startDate,
        endDate: this.form.endDate
      };

      const saved = this.editingId
        ? await firstValueFrom(this.seasonsService.update(this.editingId, payload))
        : await firstValueFrom(this.seasonsService.create(payload));

      this.upsert(saved);
      this.reset();
      this.feedbackTone.set('success');
      this.feedback.set('Temporada guardada correctamente.');
    } catch (err) {
      this.feedbackTone.set('error');
      this.feedback.set(this.resolveError(err, 'No se pudo guardar la temporada.'));
    } finally {
      this.saving.set(false);
    }
  }

  // ── Editar ────────────────────────────────────────────────────────────────

  startEdit(s: Season): void {
    this.editingId = s.seasonId;
    this.form = {
      name: s.name,
      percentageChange: s.percentageChange,
      startDate: s.startDate,
      endDate: s.endDate
    };
    this.clearFeedback();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  reset(): void {
    this.editingId = null;
    this.form = this.emptyForm();
  }

  // ── Eliminar ──────────────────────────────────────────────────────────────

  requestDelete(s: Season): void {
    this.pendingDeletion.set(s);
    this.clearFeedback();
  }

  cancelDelete(): void {
    if (!this.saving()) this.pendingDeletion.set(null);
  }

  async confirmDelete(): Promise<void> {
    const s = this.pendingDeletion();
    if (!s) return;
    this.saving.set(true);
    this.clearFeedback();
    try {
      await firstValueFrom(this.seasonsService.delete(s.seasonId));
      this.seasons.set(this.seasons().filter(x => x.seasonId !== s.seasonId));
      if (this.editingId === s.seasonId) this.reset();
      this.pendingDeletion.set(null);
      this.feedbackTone.set('success');
      this.feedback.set('Temporada eliminada correctamente.');
    } catch (err) {
      this.feedbackTone.set('error');
      this.feedback.set(this.resolveError(err, 'No se pudo eliminar la temporada.'));
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

  formatPct(n: number): string {
    return n >= 0 ? `+${n}%` : `${n}%`;
  }

  isFormValid(): boolean {
    return (
      !!this.form.name.trim() &&
      this.form.percentageChange !== null &&
      !!this.form.startDate &&
      !!this.form.endDate &&
      this.form.startDate <= this.form.endDate
    );
  }

  private emptyForm() {
    return { name: '', percentageChange: 0 as number, startDate: '', endDate: '' };
  }

  private upsert(s: Season): void {
    const list = this.seasons();
    const next = list.some(x => x.seasonId === s.seasonId)
      ? list.map(x => x.seasonId === s.seasonId ? s : x)
      : [...list, s];
    this.seasons.set(next.sort((a, b) => a.startDate.localeCompare(b.startDate)));
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
