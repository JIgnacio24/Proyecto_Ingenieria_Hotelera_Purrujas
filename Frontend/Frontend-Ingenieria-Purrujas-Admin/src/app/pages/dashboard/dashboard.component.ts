import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { AdminUser } from '../../core/auth.models';
import {
  cloneFacilitiesPageContent,
  createDefaultFacilitiesPageContent,
  FACILITIES_SERVICE_REFERENCE_LABELS,
  FacilitiesContentService,
  FacilitiesPageContent
} from '../../core/facilities-content.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent {
  private readonly authService = inject(AuthService);
  private readonly facilitiesContentService = inject(FacilitiesContentService);

  readonly loading = signal(true);
  readonly errorMessage = signal('');
  readonly profile = signal<AdminUser | null>(this.authService.currentUser());
  readonly facilitiesLoading = signal(true);
  readonly facilitiesSaving = signal(false);
  readonly facilitiesFeedback = signal('');
  readonly facilitiesFeedbackTone = signal<'success' | 'error' | ''>('');
  readonly serviceReferenceLabels = FACILITIES_SERVICE_REFERENCE_LABELS;

  facilitiesContent: FacilitiesPageContent = createDefaultFacilitiesPageContent();
  primaryListItemsText = this.facilitiesContent.primaryListItems.join('\n');
  secondaryListItemsText = this.facilitiesContent.secondaryListItems.join('\n');

  constructor() {
    void this.loadProfile();
    void this.loadFacilitiesContent();
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

  async loadFacilitiesContent(): Promise<void> {
    this.facilitiesLoading.set(true);
    this.clearFacilitiesFeedback();

    try {
      const content = await firstValueFrom(this.facilitiesContentService.getContent());
      this.applyFacilitiesContent(content);
    } catch (error) {
      this.applyFacilitiesContent(createDefaultFacilitiesPageContent());
      this.facilitiesFeedbackTone.set('error');
      this.facilitiesFeedback.set(
        this.resolveError(
          error,
          'No fue posible cargar el contenido de Facilidades. Se muestran los valores base.'
        )
      );
    } finally {
      this.facilitiesLoading.set(false);
    }
  }

  async saveFacilitiesContent(): Promise<void> {
    this.facilitiesSaving.set(true);
    this.clearFacilitiesFeedback();

    try {
      const savedContent = await firstValueFrom(
        this.facilitiesContentService.updateContent(this.buildFacilitiesContentPayload())
      );

      this.applyFacilitiesContent(savedContent);
      this.facilitiesFeedbackTone.set('success');
      this.facilitiesFeedback.set('El contenido de Facilidades se guardo correctamente.');
    } catch (error) {
      this.facilitiesFeedbackTone.set('error');
      this.facilitiesFeedback.set(
        this.resolveError(error, 'No fue posible guardar el contenido de Facilidades.')
      );

      if (error instanceof HttpErrorResponse && (error.status === 401 || error.status === 403)) {
        this.authService.logout();
      }
    } finally {
      this.facilitiesSaving.set(false);
    }
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

  private applyFacilitiesContent(content: FacilitiesPageContent): void {
    this.facilitiesContent = cloneFacilitiesPageContent(content);
    this.primaryListItemsText = this.facilitiesContent.primaryListItems.join('\n');
    this.secondaryListItemsText = this.facilitiesContent.secondaryListItems.join('\n');
  }

  private buildFacilitiesContentPayload(): FacilitiesPageContent {
    return cloneFacilitiesPageContent({
      ...this.facilitiesContent,
      primaryListItems: this.parseLines(this.primaryListItemsText),
      secondaryListItems: this.parseLines(this.secondaryListItemsText),
      serviceCards: this.facilitiesContent.serviceCards.map((service) => ({
        title: service.title.trim(),
        description: service.description.trim()
      }))
    });
  }

  private parseLines(value: string): string[] {
    return value
      .split(/\r?\n/)
      .map((line) => line.trim())
      .filter((line) => line.length > 0);
  }

  private clearFacilitiesFeedback(): void {
    this.facilitiesFeedback.set('');
    this.facilitiesFeedbackTone.set('');
  }
}
