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

  readonly menuItems = [
    { key: 'home', label: 'Home', icon: 'home' },
    { key: 'pages', label: 'Modificar Paginas', icon: 'pages' },
    { key: 'reservations', label: 'Listado de reservaciones', icon: 'reservations' },
    { key: 'rooms', label: 'Administrar habitaciones', icon: 'rooms' },
    { key: 'status', label: 'Ver estado del hotel hoy', icon: 'status' },
    { key: 'availability', label: 'Consultar disponibilidad de habitaciones', icon: 'availability' },
    { key: 'ads', label: 'Publicidad', icon: 'ads' }
  ] as const;
  readonly activeMenuItem = signal<(typeof this.menuItems)[number]['key']>('home');

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

  setActiveMenuItem(menuKey: (typeof this.menuItems)[number]['key']): void {
    this.activeMenuItem.set(menuKey);
  }

  activeMenuLabel(): string {
    return this.menuItems.find((item) => item.key === this.activeMenuItem())?.label ?? 'Home';
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

  iconPath(icon: (typeof this.menuItems)[number]['icon']): string {
    switch (icon) {
      case 'home':
        return 'M4 11.5 12 5l8 6.5V20a1 1 0 0 1-1 1h-4.5v-5h-5v5H5a1 1 0 0 1-1-1z';
      case 'pages':
        return 'M6 4h9l5 5v11a1 1 0 0 1-1 1H6a1 1 0 0 1-1-1V5a1 1 0 0 1 1-1m8 1.5V10h4.5M8 13h8M8 16h8M8 19h5';
      case 'reservations':
        return 'M7 3v3M17 3v3M5 8h14M6 5h12a1 1 0 0 1 1 1v13a1 1 0 0 1-1 1H6a1 1 0 0 1-1-1V6a1 1 0 0 1 1-1m2 7h3v3H8zm5 0h3v3h-3z';
      case 'rooms':
        return 'M5 20V7.5A1.5 1.5 0 0 1 6.5 6h11A1.5 1.5 0 0 1 19 7.5V20M3 20h18M8 10h8M8 14h5';
      case 'status':
        return 'M12 4a8 8 0 1 0 8 8h-8zM12 4a8 8 0 0 1 8 8M12 8v4l2.5 2.5';
      case 'availability':
        return 'M4 12h5l2 5 3-10 2 5h4';
      case 'ads':
        return 'M5 16V8l9-3v14zm9-6h3a2 2 0 0 1 0 4h-3M7 16v2.5A1.5 1.5 0 0 0 8.5 20H10';
      default:
        return '';
    }
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
