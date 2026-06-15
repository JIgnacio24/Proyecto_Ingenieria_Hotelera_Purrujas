import { CommonModule, DOCUMENT } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { AfterViewInit, Component, HostListener, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
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
import {
  cloneGettingTherePageContent,
  createDefaultGettingTherePageContent,
  GettingThereContentService,
  GettingTherePageContent
} from '../../core/getting-there-content.service';
import { AnalyticsComponent } from '../analytics/analytics.component';

type DashboardMenuKey =
  | 'home'
  | 'pages'
  | 'getting-there'
  | 'rooms'
  | 'analytics';

interface DashboardMenuItem {
  key: DashboardMenuKey;
  label: string;
  compactLabel: string;
  icon: string;
  targetId: string;
  route?: string;
}

interface DashboardModuleCard {
  key: string;
  title: string;
  status: string;
  description: string;
  link?: string;
  actionLabel?: string;
  group: 'editing' | 'admin';
}

interface DashboardNavigationState {
  adminFeedback?: {
    tone: 'success' | 'error';
    message: string;
  };
  [key: string]: unknown;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, AnalyticsComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements AfterViewInit {
  private readonly document = inject(DOCUMENT);
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly facilitiesContentService = inject(FacilitiesContentService);
  private readonly gettingThereContentService = inject(GettingThereContentService);
  readonly menuItems: readonly DashboardMenuItem[] = [
    {
      key: 'home',
      label: 'Inicio del panel',
      compactLabel: 'Inicio',
      icon: 'home',
      targetId: 'dashboard-home'
    },
    {
      key: 'analytics',
      label: 'Métricas del hotel',
      compactLabel: 'Métricas',
      icon: 'analytics',
      targetId: 'dashboard-metrics'
    },
    {
      key: 'pages',
      label: 'Editar facilidades',
      compactLabel: 'Facilidades',
      icon: 'pages',
      targetId: 'dashboard-content'
    },
    {
      key: 'getting-there',
      label: 'Editar cómo llegar',
      compactLabel: 'Cómo llegar',
      icon: 'getting-there',
      targetId: 'dashboard-getting-there',
      route: '/panel/como-llegar'
    },
    {
      key: 'rooms',
      label: 'Módulos del menú',
      compactLabel: 'Módulos',
      icon: 'rooms',
      targetId: 'dashboard-rooms'
    }
  ];
  readonly activeMenuItem = signal<DashboardMenuKey>('home');
  readonly mobileMenuOpen = signal(false);
  readonly moduleCards: readonly DashboardModuleCard[] = [
    {
      key: 'home-editor',
      title: 'Editar inicio',
      status: 'Editable',
      description: 'Abre la vista editable del inicio para cambiar la imagen de fondo y los textos principales.',
      link: '/panel/home',
      actionLabel: 'Abrir editor',
      group: 'editing'
    },
    {
      key: 'about-us',
      title: 'Editar Sobre nosotros',
      status: 'Editable',
      description: 'Abre la vista editable de Sobre nosotros para cambiar textos, historia, filosofía, misión, visión, valores y galería.',
      link: '/panel/sobre-nosotros',
      actionLabel: 'Abrir editor',
      group: 'editing'
    },
    {
      key: 'getting-there-editor',
      title: 'Editar cómo llegar',
      status: 'Editable',
      description: 'Abre el editor de Cómo llegar para actualizar el encabezado, las coordenadas y las indicaciones.',
      link: '/panel/como-llegar',
      actionLabel: 'Abrir editor',
      group: 'editing'
    },
    {
      key: 'reservations',
      title: 'Administración de Reservaciones',
      status: 'Disponible',
      description: 'Ver y gestionar todas las reservas en línea. Actualiza, elimina y visualiza reservaciones.',
      link: '/panel/reservas',
      actionLabel: 'Ver reservaciones',
      group: 'admin'
    },
    {
      key: 'rooms',
      title: 'Tipos de habitación',
      status: 'Disponible',
      description: 'Administra los tipos de habitación, sus nombres y tarifas base desde una vista dedicada.',
      link: '/panel/tipos-habitacion',
      actionLabel: 'Administrar tipos',
      group: 'admin'
    },
    {
      key: 'room-management',
      title: 'Gestión de habitaciones',
      status: 'Disponible',
      description: 'Registra cada habitación física del hotel, asigna su tipo y cambia su estado operativo.',
      link: '/panel/habitaciones',
      actionLabel: 'Administrar habitaciones',
      group: 'admin'
    },
    {
      key: 'seasons',
      title: 'Temporadas',
      status: 'Disponible',
      description: 'Gestiona los periodos de precio diferenciado: crea, edita y elimina temporadas altas y bajas.',
      link: '/panel/temporadas',
      actionLabel: 'Administrar temporadas',
      group: 'admin'
    },
    {
      key: 'promotions',
      title: 'Ofertas especiales',
      status: 'Disponible',
      description: 'Crea, edita y elimina descuentos y promociones especiales por tipo de habitación.',
      link: '/panel/ofertas',
      actionLabel: 'Administrar ofertas',
      group: 'admin'
    },
    {
      key: 'ads',
      title: 'Publicidad',
      status: 'Interfaz pendiente',
      description: 'Módulo en construcción. Aquí se administrará la publicidad visible en el sitio del cliente.',
      group: 'admin'
    }
  ];

  get editingModuleCards(): readonly DashboardModuleCard[] {
    return this.moduleCards.filter((m) => m.group === 'editing');
  }

  get adminModuleCards(): readonly DashboardModuleCard[] {
    return this.moduleCards.filter((m) => m.group === 'admin');
  }

  readonly loading = signal(true);
  readonly errorMessage = signal('');
  readonly dashboardFeedback = signal('');
  readonly dashboardFeedbackTone = signal<'success' | 'error' | ''>('');
  readonly profile = signal<AdminUser | null>(this.authService.currentUser());
  readonly facilitiesLoading = signal(true);
  readonly facilitiesSaving = signal(false);
  readonly facilitiesFeedback = signal('');
  readonly facilitiesFeedbackTone = signal<'success' | 'error' | ''>('');
  readonly gettingThereLoading = signal(true);
  readonly gettingThereSaving = signal(false);
  readonly gettingThereModalOpen = signal(false);
  readonly gettingThereFeedback = signal('');
  readonly gettingThereFeedbackTone = signal<'success' | 'error' | ''>('');
  readonly serviceReferenceLabels = FACILITIES_SERVICE_REFERENCE_LABELS;
  facilitiesContent: FacilitiesPageContent = createDefaultFacilitiesPageContent();
  primaryListItemsText = this.facilitiesContent.primaryListItems.join('\n');
  secondaryListItemsText = this.facilitiesContent.secondaryListItems.join('\n');
  gettingThereContent: GettingTherePageContent = createDefaultGettingTherePageContent();
  gettingThereDirectionsItemsText = this.gettingThereContent.directionsItems.join('\n');
  private gettingThereModalSnapshot: GettingTherePageContent | null = null;
  constructor() {
    this.consumeNavigationFeedback();
    void this.loadProfile();
    void this.loadFacilitiesContent();
    void this.loadGettingThereContent();
  }

  ngAfterViewInit(): void {
    this.syncActiveMenuItem();
  }

  async loadProfile(): Promise<void> {
    this.loading.set(true);
    this.errorMessage.set('');

    try {
      const user = await firstValueFrom(this.authService.fetchProfile());
      this.profile.set(user);
    } catch (error) {
      this.errorMessage.set(this.resolveError(error, 'No fue posible validar la sesión.'));

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
          'No fue posible cargar el contenido de facilidades. Se muestran los valores predeterminados.'
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
      this.facilitiesFeedback.set('El contenido de facilidades se guardó correctamente.');
    } catch (error) {
      this.facilitiesFeedbackTone.set('error');
      this.facilitiesFeedback.set(
        this.resolveError(error, 'No fue posible guardar el contenido de facilidades.')
      );

      if (error instanceof HttpErrorResponse && (error.status === 401 || error.status === 403)) {
        this.authService.logout();
      }
    } finally {
      this.facilitiesSaving.set(false);
    }
  }

  async loadGettingThereContent(): Promise<void> {
    this.gettingThereLoading.set(true);
    this.clearGettingThereFeedback();

    try {
      const content = await firstValueFrom(this.gettingThereContentService.getContent());
      this.applyGettingThereContent(content);
    } catch (error) {
      this.applyGettingThereContent(createDefaultGettingTherePageContent());
      this.gettingThereFeedbackTone.set('error');
      this.gettingThereFeedback.set(
        this.resolveError(
          error,
          'No fue posible cargar el contenido de Cómo llegar. Se muestran los valores predeterminados.'
        )
      );
    } finally {
      this.gettingThereLoading.set(false);
    }
  }

  async saveGettingThereContent(): Promise<void> {
    this.gettingThereSaving.set(true);
    this.clearGettingThereFeedback();

    try {
      const savedContent = await firstValueFrom(
        this.gettingThereContentService.updateContent(this.buildGettingThereContentPayload())
      );

      this.applyGettingThereContent(savedContent);
      this.gettingThereFeedbackTone.set('success');
      this.gettingThereFeedback.set('El contenido de Cómo llegar se guardó correctamente.');
      this.closeGettingThereModal(false);
    } catch (error) {
      this.gettingThereFeedbackTone.set('error');
      this.gettingThereFeedback.set(
        this.resolveError(error, 'No fue posible guardar el contenido de Cómo llegar.')
      );

      if (error instanceof HttpErrorResponse && (error.status === 401 || error.status === 403)) {
        this.authService.logout();
      }
    } finally {
      this.gettingThereSaving.set(false);
    }
  }

  setActiveMenuItem(menuKey: DashboardMenuKey): void {
    // El menu lateral navega por secciones del dashboard sin cambiar de ruta.
    this.activeMenuItem.set(menuKey);
    this.closeMobileMenu();

    if (menuKey === 'getting-there') {
      this.openGettingThereModal();
      return;
    }

    const targetId = this.menuItems.find((item) => item.key === menuKey)?.targetId;

    if (!targetId) {
      return;
    }

    this.scrollToDashboardSection(targetId);
  }

  openGettingThereModal(): void {
    this.gettingThereModalSnapshot = this.buildGettingThereContentPayload();
    this.clearGettingThereFeedback();
    this.gettingThereModalOpen.set(true);
  }

  closeGettingThereModal(discardChanges = true): void {
    if (discardChanges && this.gettingThereSaving()) {
      return;
    }

    if (discardChanges && this.gettingThereModalSnapshot) {
      this.applyGettingThereContent(this.gettingThereModalSnapshot);
    }

    this.gettingThereModalSnapshot = null;
    this.gettingThereModalOpen.set(false);
  }

  activeMenuLabel(): string {
    return this.menuItems.find((item) => item.key === this.activeMenuItem())?.label ?? 'Inicio';
  }

  moduleSectionId(menuKey: DashboardModuleCard['key']): string {
    return this.menuItems.find((item) => item.key === menuKey)?.targetId ?? '';
  }

  formatDate(value: string | null | undefined): string {
    if (!value) {
      return 'Aún sin registro';
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
      case 'analytics':
        return 'M18 20V10M12 20V4M6 20v-6M3 20h18';
      case 'pages':
        return 'M6 4h9l5 5v11a1 1 0 0 1-1 1H6a1 1 0 0 1-1-1V5a1 1 0 0 1 1-1m8 1.5V10h4.5M8 13h8M8 16h8M8 19h5';
      case 'getting-there':
        return 'M12 21s7-5.2 7-11a7 7 0 1 0-14 0c0 5.8 7 11 7 11m0-8a3 3 0 1 0 0-6 3 3 0 0 0 0 6';
      case 'rooms':
        return 'M4 6h16M4 12h16M4 18h16M8 6v12M16 6v12';
      default:
        return '';
    }
  }

  @HostListener('window:scroll')
  onWindowScroll(): void {
    this.syncActiveMenuItem();
  }

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    if (this.gettingThereModalOpen()) {
      this.closeGettingThereModal();
    }
  }

  toggleMobileMenu(): void {
    this.mobileMenuOpen.update((open) => !open);
  }

  closeMobileMenu(): void {
    this.mobileMenuOpen.set(false);
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

  private consumeNavigationFeedback(): void {
    const state = this.currentNavigationState();
    const feedback = state?.adminFeedback;

    if (
      !feedback ||
      typeof feedback.message !== 'string' ||
      (feedback.tone !== 'success' && feedback.tone !== 'error')
    ) {
      return;
    }

    this.dashboardFeedbackTone.set(feedback.tone);
    this.dashboardFeedback.set(feedback.message);
    this.clearNavigationFeedbackState();
  }

  private currentNavigationState(): DashboardNavigationState | null {
    const state = this.router.getCurrentNavigation()?.extras.state
      ?? this.document.defaultView?.history.state
      ?? null;

    return state as DashboardNavigationState | null;
  }

  private clearNavigationFeedbackState(): void {
    const view = this.document.defaultView;

    if (!view?.history.state) {
      return;
    }

    const { adminFeedback, ...remainingState } = view.history.state as DashboardNavigationState;
    view.history.replaceState(remainingState, this.document.title, view.location.href);
  }

  private applyFacilitiesContent(content: FacilitiesPageContent): void {
    this.facilitiesContent = cloneFacilitiesPageContent(content);
    this.primaryListItemsText = this.facilitiesContent.primaryListItems.join('\n');
    this.secondaryListItemsText = this.facilitiesContent.secondaryListItems.join('\n');
  }

  private buildFacilitiesContentPayload(): FacilitiesPageContent {
    // Los campos multilinea del formulario se guardan como arreglos limpios en el JSON.
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

  private applyGettingThereContent(content: GettingTherePageContent): void {
    this.gettingThereContent = cloneGettingTherePageContent(content);
    this.gettingThereDirectionsItemsText = this.gettingThereContent.directionsItems.join('\n');
  }

  private buildGettingThereContentPayload(): GettingTherePageContent {
    return cloneGettingTherePageContent({
      ...this.gettingThereContent,
      directionsItems: this.parseLines(this.gettingThereDirectionsItemsText)
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

  private clearGettingThereFeedback(): void {
    this.gettingThereFeedback.set('');
    this.gettingThereFeedbackTone.set('');
  }

  private syncActiveMenuItem(): void {
    // Mantiene resaltado el boton lateral correspondiente a la seccion visible.
    const marker = this.dashboardScrollOffset() + 24;
    const sections = this.menuItems
      .map((item) => ({
        key: item.key,
        element: this.document.getElementById(item.targetId)
      }))
      .filter((section): section is { key: DashboardMenuKey; element: HTMLElement } => !!section.element);

    if (sections.length === 0) {
      return;
    }

    const visibleSection = sections.find(({ element }) => {
      const rect = element.getBoundingClientRect();
      return rect.top <= marker && rect.bottom > marker;
    });

    if (visibleSection) {
      if (this.activeMenuItem() !== visibleSection.key) {
        this.activeMenuItem.set(visibleSection.key);
      }

      return;
    }

    const closestSection = sections.reduce((closest, current) => {
      const currentDistance = Math.abs(current.element.getBoundingClientRect().top - marker);
      const closestDistance = Math.abs(closest.element.getBoundingClientRect().top - marker);
      return currentDistance < closestDistance ? current : closest;
    });

    if (this.activeMenuItem() !== closestSection.key) {
      this.activeMenuItem.set(closestSection.key);
    }
  }

  private scrollToDashboardSection(targetId: string): void {
    const target = this.document.getElementById(targetId);
    const view = this.document.defaultView;

    if (!target || !view) {
      return;
    }

    const top = target.getBoundingClientRect().top + view.scrollY - this.dashboardScrollOffset();
    view.scrollTo({
      top: Math.max(top, 0),
      left: 0,
      behavior: 'smooth'
    });
  }

  private dashboardScrollOffset(): number {
    const view = this.document.defaultView;
    const sideNav = this.document.querySelector('.side-nav');

    if (
      view &&
      view.matchMedia('(max-width: 1200px)').matches &&
      sideNav instanceof HTMLElement
    ) {
      return sideNav.offsetHeight + 16;
    }

    return 18;
  }
}
