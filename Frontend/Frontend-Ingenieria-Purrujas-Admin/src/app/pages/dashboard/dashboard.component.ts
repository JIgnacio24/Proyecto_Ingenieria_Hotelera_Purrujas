import { CommonModule, DOCUMENT } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { AfterViewInit, Component, HostListener, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
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

type DashboardMenuKey =
  | 'home'
  | 'pages'
  | 'about-us'
  | 'reservations'
  | 'rooms'
  | 'status'
  | 'availability'
  | 'ads';

interface DashboardMenuItem {
  key: DashboardMenuKey;
  label: string;
  compactLabel: string;
  icon: string;
  targetId: string;
  route?: string;
}

interface DashboardModuleCard {
  key: 'about-us' | Extract<DashboardMenuKey, 'reservations' | 'rooms' | 'availability' | 'ads'>;
  title: string;
  status: string;
  description: string;
  link?: string;
  actionLabel?: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements AfterViewInit {
  private readonly document = inject(DOCUMENT);
  private readonly authService = inject(AuthService);
  private readonly facilitiesContentService = inject(FacilitiesContentService);

  readonly menuItems: readonly DashboardMenuItem[] = [
    {
      key: 'home',
      label: 'Inicio del panel',
      compactLabel: 'Inicio',
      icon: 'home',
      targetId: 'dashboard-home'
    },
    {
      key: 'status',
      label: 'Ver estado del hotel hoy',
      compactLabel: 'Estado',
      icon: 'status',
      targetId: 'dashboard-status'
    },
    {
      key: 'pages',
      label: 'Modificar paginas',
      compactLabel: 'Paginas',
      icon: 'pages',
      targetId: 'dashboard-content'
    },
    {
      key: 'about-us',
      label: 'Editar Sobre Nosotros',
      compactLabel: 'Sobre',
      icon: 'about-us',
      // Acceso lateral a la tarjeta del dashboard; la tarjeta mantiene el enlace al editor completo.
      targetId: 'dashboard-about-us'
    },
    {
      key: 'reservations',
      label: 'Listado de reservaciones',
      compactLabel: 'Reservas',
      icon: 'reservations',
      targetId: 'dashboard-reservations'
    },
    {
      key: 'rooms',
      label: 'Administrar habitaciones',
      compactLabel: 'Habitaciones',
      icon: 'rooms',
      targetId: 'dashboard-rooms'
    },
    {
      key: 'availability',
      label: 'Consultar disponibilidad de habitaciones',
      compactLabel: 'Disponibilidad',
      icon: 'availability',
      targetId: 'dashboard-availability'
    },
    {
      key: 'ads',
      label: 'Publicidad',
      compactLabel: 'Publicidad',
      icon: 'ads',
      targetId: 'dashboard-ads'
    }
  ];
  readonly activeMenuItem = signal<DashboardMenuKey>('home');
  readonly moduleCards: readonly DashboardModuleCard[] = [
    {
      key: 'about-us',
      title: 'Editar Sobre Nosotros',
      status: 'Editable',
      description:
        'Abre la vista editable de Sobre Nosotros para cambiar textos, historia, filosofia, mision, vision, valores y galeria.',
      link: '/panel/sobre-nosotros',
      actionLabel: 'Abrir editor'
    },
    {
      key: 'reservations',
      title: 'Listado de reservaciones',
      status: 'Pendiente de interfaz',
      description:
        'Espacio reservado para mostrar reservas, filtros por fecha y detalle del cliente cuando ese modulo se conecte.'
    },
    {
      key: 'rooms',
      title: 'Administrar habitaciones',
      status: 'Pendiente de interfaz',
      description:
        'Aqui ira la administracion de tipos de habitacion, tarifas base y estado operativo cuando el modulo exista.'
    },
    {
      key: 'availability',
      title: 'Disponibilidad de habitaciones',
      status: 'Pendiente de interfaz',
      description:
        'Zona prevista para consultar ocupacion, cupos por fecha y validaciones de disponibilidad desde el panel.'
    },
    {
      key: 'ads',
      title: 'Publicidad',
      status: 'Interfaz pendiente',
      description:
        'El menu ya apunta a esta seccion. Falta construir aqui el CRUD para administrar la publicidad del sitio.'
    }
  ];

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

  setActiveMenuItem(menuKey: DashboardMenuKey): void {
    // El menu lateral navega por secciones del dashboard sin cambiar de ruta.
    this.activeMenuItem.set(menuKey);
    const targetId = this.menuItems.find((item) => item.key === menuKey)?.targetId;

    if (!targetId) {
      return;
    }

    this.document.getElementById(targetId)?.scrollIntoView({
      behavior: 'smooth',
      block: 'start'
    });
  }

  activeMenuLabel(): string {
    return this.menuItems.find((item) => item.key === this.activeMenuItem())?.label ?? 'Home';
  }

  moduleSectionId(menuKey: DashboardModuleCard['key']): string {
    if (menuKey === 'about-us') {
      return 'dashboard-about-us';
    }

    return this.menuItems.find((item) => item.key === menuKey)?.targetId ?? '';
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
      case 'about-us':
        return 'M12 12a4 4 0 1 0-4-4 4 4 0 0 0 4 4m-7 9a7 7 0 0 1 14 0M4 4h16v16H4z';
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

  @HostListener('window:scroll')
  onWindowScroll(): void {
    this.syncActiveMenuItem();
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

  private syncActiveMenuItem(): void {
    // Mantiene resaltado el boton lateral correspondiente a la seccion visible.
    const marker = 180;
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
}
