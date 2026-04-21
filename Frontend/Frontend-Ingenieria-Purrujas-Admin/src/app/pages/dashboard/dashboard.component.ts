import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { AdminUser } from '../../core/auth.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent {
  private readonly authService = inject(AuthService);

  readonly loading = signal(true);
  readonly errorMessage = signal('');
  readonly profile = signal<AdminUser | null>(this.authService.currentUser());

  constructor() {
    void this.loadProfile();
  }

  async loadProfile(): Promise<void> {
    this.loading.set(true);
    this.errorMessage.set('');

    try {
      const user = await firstValueFrom(this.authService.fetchProfile());
      this.profile.set(user);
    } catch (error) {
      this.errorMessage.set(this.resolveError(error, 'No fue posible validar la sesion.'));

      if (error instanceof HttpErrorResponse && (error.status === 401 || error.status === 403)) {
        this.authService.logout();
        return;
      }
    } finally {
      this.loading.set(false);
    }
  }

  logout(): void {
    this.authService.logout();
  }

  formatDate(value: string | null | undefined): string {
    if (!value) {
      return 'Aun sin registro';
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
