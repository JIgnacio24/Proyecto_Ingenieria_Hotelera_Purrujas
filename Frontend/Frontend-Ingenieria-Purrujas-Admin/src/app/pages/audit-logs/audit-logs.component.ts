import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AdminAuditLog, AdminAuditLogsService } from '../../core/admin-audit-logs.service';

@Component({
  selector: 'app-audit-logs',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './audit-logs.component.html',
  styleUrl: './audit-logs.component.css'
})
export class AuditLogsComponent {
  private readonly auditLogsService = inject(AdminAuditLogsService);

  readonly loading = signal(true);
  readonly feedback = signal('');
  readonly feedbackTone = signal<'success' | 'error' | ''>('');
  readonly logs = signal<AdminAuditLog[]>([]);

  take = 100;

  constructor() {
    void this.loadLogs();
  }

  async loadLogs(): Promise<void> {
    this.loading.set(true);
    this.clearFeedback();

    try {
      const logs = await firstValueFrom(this.auditLogsService.getRecent(this.take));
      this.logs.set(logs);
      this.feedbackTone.set('success');
      this.feedback.set(`Se cargaron ${logs.length} registros de bitacora.`);
    } catch (error) {
      this.feedbackTone.set('error');
      this.feedback.set(this.resolveError(error, 'No fue posible cargar las bitacoras.'));
    } finally {
      this.loading.set(false);
    }
  }

  userLabel(log: AdminAuditLog): string {
    const fullName = log.fullName?.trim() || 'Usuario no disponible';
    const username = log.username?.trim();
    return username ? `${fullName} (${username})` : fullName;
  }

  formatDate(value: string | null | undefined): string {
    if (!value) {
      return 'Sin fecha';
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return value;
    }

    return new Intl.DateTimeFormat('es-CR', {
      dateStyle: 'medium',
      timeStyle: 'short'
    }).format(date);
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
