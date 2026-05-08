import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

export interface GettingTherePageContent {
  sectionTag: string;
  sectionTitle: string;
  sectionSubtext: string;
  coordinatesTitle: string;
  coordinatesDescription: string;
  directionsItems: string[];
  mapButtonLabel: string;
}

const DEFAULT_GETTING_THERE_PAGE_CONTENT: GettingTherePageContent = {
  sectionTag: 'Visítanos',
  sectionTitle: '¿Cómo llegar?',
  sectionSubtext: 'A 45 minutos de San José, en las faldas del Volcán Turrialba.',
  coordinatesTitle: 'Coordenadas',
  coordinatesDescription: '9.975878207007307° N,83.770258333651° W · Las Purrujas, Cartago.',
  directionsItems: [
    'Ruta 32 hasta Turrialba, luego desvío a La Pastora.',
    'Transporte privado disponible desde el aeropuerto SJO.',
    'Parqueo gratuito y seguro dentro de la propiedad.'
  ],
  mapButtonLabel: 'Abrir en Google Maps'
};

@Injectable({ providedIn: 'root' })
export class GettingThereContentService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api';

  getContent(): Observable<GettingTherePageContent> {
    return this.http
      .get<GettingTherePageContent>(`${this.apiUrl}/getting-there-content`, {
        params: { _: Date.now().toString() }
      })
      .pipe(
      map((content) => normalizeGettingTherePageContent(content)),
      catchError((error) => {
        console.error('No fue posible cargar el contenido de "Como llegar" desde la API.', error);
        return of(createDefaultGettingTherePageContent());
      })
      );
  }
}

export function createDefaultGettingTherePageContent(): GettingTherePageContent {
  return normalizeGettingTherePageContent(DEFAULT_GETTING_THERE_PAGE_CONTENT);
}

function normalizeGettingTherePageContent(
  content: Partial<GettingTherePageContent> | null | undefined
): GettingTherePageContent {
  const defaults = DEFAULT_GETTING_THERE_PAGE_CONTENT;

  return {
    sectionTag: coalesce(content?.sectionTag, defaults.sectionTag),
    sectionTitle: coalesce(content?.sectionTitle, defaults.sectionTitle),
    sectionSubtext: coalesce(content?.sectionSubtext, defaults.sectionSubtext),
    coordinatesTitle: coalesce(content?.coordinatesTitle, defaults.coordinatesTitle),
    coordinatesDescription: coalesce(
      content?.coordinatesDescription,
      defaults.coordinatesDescription
    ),
    directionsItems: sanitizeList(content?.directionsItems, defaults.directionsItems),
    mapButtonLabel: coalesce(content?.mapButtonLabel, defaults.mapButtonLabel)
  };
}

function sanitizeList(values: string[] | undefined, fallback: string[]): string[] {
  const normalizedValues = (values ?? [])
    .map((value) => value?.trim() ?? '')
    .filter((value) => value.length > 0);

  return normalizedValues.length > 0 ? [...normalizedValues] : [...fallback];
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
