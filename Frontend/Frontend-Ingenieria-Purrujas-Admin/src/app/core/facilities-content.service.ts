import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

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

export const FACILITIES_SERVICE_REFERENCE_LABELS = [
  'Habitaciones tematicas',
  'Restaurante La Ceiba',
  'Piscina natural',
  'Senderos ecologicos',
  'Salon de eventos',
  'Spa',
  'Tours a volcanes',
  'Birdwatching',
  'Gastronomia tipica',
  'Transporte',
  'Wi-Fi',
  'Atencion personalizada'
] as const;

const DEFAULT_FACILITIES_PAGE_CONTENT: FacilitiesPageContent = {
  sectionTag: 'Lo que nos distingue',
  sectionTitle: 'Caracteristicas Principales',
  highlightTitle: 'Ubicacion Privilegiada',
  highlightDescription:
    'Situado a solo 45 minutos de San Jose, en las verdes montanas de Cartago, el hotel ofrece vistas panoramicas al Volcan Turrialba y esta rodeado de bosques nubosos y rios cristalinos. Una combinacion unica de accesibilidad y tranquilidad absoluta.',
  primaryListTitle: 'Instalaciones',
  primaryListItems: [
    '18 habitaciones tematicas',
    'Restaurante "La Ceiba"',
    'Piscina natural de manantial',
    'Senderos ecologicos (5 km)',
    'Salon de eventos',
    'Spa con plantas locales'
  ],
  secondaryListTitle: 'Servicios Destacados',
  secondaryListItems: [
    'Tours al Volcan Turrialba e Irazu',
    'Birdwatching con guias certificados',
    'Talleres de gastronomia tipica',
    'Transporte desde San Jose',
    'Wi-Fi de alta velocidad',
    'Atencion personalizada 24/7'
  ],
  serviceCards: [
    {
      title: '18 habitaciones tematicas',
      description:
        'Ambientes con personalidad propia, balcones al bosque nuboso y textiles artesanales inspirados en Cartago.'
    },
    {
      title: 'Restaurante "La Ceiba"',
      description:
        'Cocina de finca a la mesa, cafe chorreado y menus de temporada que celebran los sabores locales.'
    },
    {
      title: 'Piscina natural de manantial',
      description:
        'Agua cristalina, temperatura agradable y vistas verdes para recargar energia de forma natural.'
    },
    {
      title: 'Senderos ecologicos (5 km)',
      description:
        'Rutas senalizadas entre bosque nuboso, ideales para caminatas al amanecer y observacion de flora.'
    },
    {
      title: 'Salon de eventos',
      description:
        'Espacio versatil con luz natural, perfecto para retiros corporativos, bodas boutique y talleres.'
    },
    {
      title: 'Spa con plantas locales',
      description:
        'Tratamientos herbales, masajes relajantes y aromaterapia con esencias del bosque costarricense.'
    },
    {
      title: 'Tours al Volcan Turrialba e Irazu',
      description:
        'Excursiones guiadas para explorar dos volcanes iconicos con logistica y transporte incluidos.'
    },
    {
      title: 'Birdwatching con guias certificados',
      description:
        'Avistamiento de purrujas y mas de 120 especies con especialistas locales y equipo optico.'
    },
    {
      title: 'Talleres de gastronomia tipica',
      description:
        'Aprende a preparar tortillas palmeadas, gallo pinto y salsas caseras con cocineras de la zona.'
    },
    {
      title: 'Transporte desde San Jose',
      description:
        'Traslados seguros puerta a puerta para que llegues sin preocupaciones desde el aeropuerto o la ciudad.'
    },
    {
      title: 'Wi-Fi de alta velocidad',
      description:
        'Conectividad confiable en habitaciones y areas comunes para trabajar o compartir tu experiencia.'
    },
    {
      title: 'Atencion personalizada 24/7',
      description:
        'Equipo disponible todo el dia para ayudarte con reservas, recomendaciones y soporte durante tu estadia.'
    }
  ]
};

@Injectable({ providedIn: 'root' })
export class FacilitiesContentService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl;

  getContent(): Observable<FacilitiesPageContent> {
    return this.http.get<FacilitiesPageContent>(`${this.apiBaseUrl}/facilities-content`);
  }

  updateContent(payload: FacilitiesPageContent): Observable<FacilitiesPageContent> {
    return this.http.put<FacilitiesPageContent>(`${this.apiBaseUrl}/facilities-content`, payload);
  }
}

export function createDefaultFacilitiesPageContent(): FacilitiesPageContent {
  return normalizeFacilitiesPageContent(DEFAULT_FACILITIES_PAGE_CONTENT);
}

export function cloneFacilitiesPageContent(content: FacilitiesPageContent): FacilitiesPageContent {
  return normalizeFacilitiesPageContent(content);
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
      'Descripcion pendiente.'
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
