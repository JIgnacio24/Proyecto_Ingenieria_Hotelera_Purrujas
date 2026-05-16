import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

export interface FacilitiesServiceText {
  title: string;
  description: string;
}

export interface FacilitiesPageContent {
  sectionTag: string;
  sectionTitle: string;
  highlightTitle: string;
  highlightDescription: string;
  primaryListTitle: string;
  primaryListItems: string[];
  secondaryListTitle: string;
  secondaryListItems: string[];
  serviceCards: FacilitiesServiceText[];
}

const DEFAULT_FACILITIES_PAGE_CONTENT: FacilitiesPageContent = {
  sectionTag: 'Lo que nos distingue',
  sectionTitle: 'Características principales',
  highlightTitle: 'Ubicación privilegiada',
  highlightDescription:
    'Situado a solo 45 minutos de San José, en las verdes montañas de Cartago, el hotel ofrece vistas panorámicas al Volcán Turrialba y está rodeado de bosques nubosos y ríos cristalinos. Una combinación única de accesibilidad y tranquilidad absoluta.',
  primaryListTitle: 'Instalaciones',
  primaryListItems: [
    '18 habitaciones temáticas',
    'Restaurante "La Ceiba"',
    'Piscina natural de manantial',
    'Senderos ecológicos (5 km)',
    'Salón de eventos',
    'Spa con plantas locales'
  ],
  secondaryListTitle: 'Servicios destacados',
  secondaryListItems: [
    'Tours al Volcán Turrialba e Irazú',
    'Birdwatching con guías certificados',
    'Talleres de gastronomía típica',
    'Transporte desde San José',
    'Wi-Fi de alta velocidad',
    'Atención personalizada 24/7'
  ],
  serviceCards: [
    {
      title: '18 habitaciones temáticas',
      description:
        'Ambientes con personalidad propia, balcones al bosque nuboso y textiles artesanales inspirados en Cartago.'
    },
    {
      title: 'Restaurante "La Ceiba"',
      description:
        'Cocina de finca a la mesa, café chorreado y menús de temporada que celebran los sabores locales.'
    },
    {
      title: 'Piscina natural de manantial',
      description:
        'Agua cristalina, temperatura agradable y vistas verdes para recargar energía de forma natural.'
    },
    {
      title: 'Senderos ecológicos (5 km)',
      description:
        'Rutas señalizadas entre bosque nuboso, ideales para caminatas al amanecer y observación de flora.'
    },
    {
      title: 'Salón de eventos',
      description:
        'Espacio versátil con luz natural, perfecto para retiros corporativos, bodas boutique y talleres.'
    },
    {
      title: 'Spa con plantas locales',
      description:
        'Tratamientos herbales, masajes relajantes y aromaterapia con esencias del bosque costarricense.'
    },
    {
      title: 'Tours al Volcán Turrialba e Irazú',
      description:
        'Excursiones guiadas para explorar dos volcanes icónicos con logística y transporte incluidos.'
    },
    {
      title: 'Birdwatching con guías certificados',
      description:
        'Avistamiento de purrujas y más de 120 especies con especialistas locales y equipo óptico.'
    },
    {
      title: 'Talleres de gastronomía típica',
      description:
        'Aprende a preparar tortillas palmeadas, gallo pinto y salsas caseras con cocineras de la zona.'
    },
    {
      title: 'Transporte desde San José',
      description:
        'Traslados seguros puerta a puerta para que llegues sin preocupaciones desde el aeropuerto o la ciudad.'
    },
    {
      title: 'Wi-Fi de alta velocidad',
      description:
        'Conectividad confiable en habitaciones y áreas comunes para trabajar o compartir tu experiencia.'
    },
    {
      title: 'Atención personalizada 24/7',
      description:
        'Equipo disponible todo el día para ayudarte con reservas, recomendaciones y soporte durante tu estadía.'
    }
  ]
};

@Injectable({ providedIn: 'root' })
export class FacilitiesContentService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5232/api';

  getContent(): Observable<FacilitiesPageContent> {
    return this.http.get<FacilitiesPageContent>(`${this.apiUrl}/facilities-content`).pipe(
      map((content) => normalizeFacilitiesPageContent(content)),
      catchError(() => of(createDefaultFacilitiesPageContent()))
    );
  }
}

export function createDefaultFacilitiesPageContent(): FacilitiesPageContent {
  return normalizeFacilitiesPageContent(DEFAULT_FACILITIES_PAGE_CONTENT);
}

function normalizeFacilitiesPageContent(
  content: Partial<FacilitiesPageContent> | null | undefined
): FacilitiesPageContent {
  const defaults = DEFAULT_FACILITIES_PAGE_CONTENT;

  return {
    sectionTag: coalesce(content?.sectionTag, defaults.sectionTag),
    sectionTitle: coalesce(content?.sectionTitle, defaults.sectionTitle),
    highlightTitle: coalesce(content?.highlightTitle, defaults.highlightTitle),
    highlightDescription: coalesce(content?.highlightDescription, defaults.highlightDescription),
    primaryListTitle: coalesce(content?.primaryListTitle, defaults.primaryListTitle),
    primaryListItems: sanitizeList(content?.primaryListItems, defaults.primaryListItems),
    secondaryListTitle: coalesce(content?.secondaryListTitle, defaults.secondaryListTitle),
    secondaryListItems: sanitizeList(content?.secondaryListItems, defaults.secondaryListItems),
    serviceCards: sanitizeServiceCards(content?.serviceCards, defaults.serviceCards)
  };
}

function sanitizeList(values: string[] | undefined, fallback: string[]): string[] {
  const normalizedValues = (values ?? [])
    .map((value) => value?.trim() ?? '')
    .filter((value) => value.length > 0);

  return normalizedValues.length > 0 ? [...normalizedValues] : [...fallback];
}

function sanitizeServiceCards(
  cards: FacilitiesServiceText[] | undefined,
  fallback: FacilitiesServiceText[]
): FacilitiesServiceText[] {
  const normalizedCards = (cards ?? []).map((card) => ({
    title: card?.title?.trim() ?? '',
    description: card?.description?.trim() ?? ''
  }));

  const totalCards = Math.max(normalizedCards.length, fallback.length);

  return Array.from({ length: totalCards }, (_, index) => ({
    title: coalesce(normalizedCards[index]?.title, fallback[index]?.title, `Servicio ${index + 1}`),
    description: coalesce(
      normalizedCards[index]?.description,
      fallback[index]?.description,
      'Descripción pendiente.'
    )
  }));
}

function coalesce(...values: Array<string | undefined>): string {
  for (const value of values) {
    const normalized = value?.trim();
    if (normalized) {
      return normalized;
    }
  }

  return '';
}
