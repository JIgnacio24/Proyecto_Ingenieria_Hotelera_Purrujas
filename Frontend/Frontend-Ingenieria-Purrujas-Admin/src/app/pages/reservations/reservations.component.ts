import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AdminReservationResponse, ReservationService } from '../../core/reservation.service';

type StatusFilter = 'todas' | 'Pendiente' | 'Confirmada' | 'Finalizada' | 'Cancelada';

@Component({
  selector: 'app-reservations',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './reservations.component.html',
  styleUrl: './reservations.component.css'
})
export class ReservationsComponent {
  private readonly reservationService = inject(ReservationService);

  readonly loading = signal(true);
  readonly error = signal('');
  readonly reservations = signal<AdminReservationResponse[]>([]);
  readonly statusFilter = signal<StatusFilter>('todas');
  readonly updatingId = signal<number | null>(null);
  readonly updateFeedback = signal('');
  readonly updateFeedbackTone = signal<'success' | 'error' | ''>('');

  readonly statusOptions: StatusFilter[] = ['todas', 'Pendiente', 'Confirmada', 'Finalizada', 'Cancelada'];
  readonly changeableStatuses = ['Pendiente', 'Confirmada', 'Cancelada'];

  constructor() {
    void this.loadReservations();
  }

  get filteredReservations(): AdminReservationResponse[] {
    const filter = this.statusFilter();
    if (filter === 'todas') return this.reservations();
    return this.reservations().filter(r => r.status === filter);
  }

  setFilter(status: StatusFilter): void {
    this.statusFilter.set(status);
  }

  countByStatus(status: StatusFilter): number {
    if (status === 'todas') return this.reservations().length;
    return this.reservations().filter(r => r.status === status).length;
  }

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

  formatDate(dateStr: string): string {
    if (!dateStr) return '—';
    const d = new Date(dateStr);
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
      const data = await firstValueFrom(this.reservationService.getAll());
      this.reservations.set(data);
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
