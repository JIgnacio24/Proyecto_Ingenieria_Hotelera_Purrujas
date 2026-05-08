import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface HomePageContent {
  // Contrato recibido desde la API para no mantener textos quemados en el hero.
  heroEyebrow: string;
  heroTitle: string;
  heroSubtitle: string;
}

@Injectable({ providedIn: 'root' })
export class HomeContentService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api';

  getContent(): Observable<HomePageContent> {
    return this.http
      .get<Partial<HomePageContent>>(`${this.apiUrl}/home-content`)
      .pipe(map((content) => normalizeHomePageContent(content)));
  }
}

export function createDefaultHomePageContent(): HomePageContent {
  return {
    heroEyebrow: 'Bienvenido',
    heroTitle: 'Hotel Las Purrujas',
    heroSubtitle: 'Donde la naturaleza te abraza y Costa Rica te enamora.'
  };
}

export function normalizeHomePageContent(
  content: Partial<HomePageContent> | null | undefined
): HomePageContent {
  const fallback = createDefaultHomePageContent();

  return {
    heroEyebrow: normalizeText(content?.heroEyebrow, fallback.heroEyebrow),
    heroTitle: normalizeText(content?.heroTitle, fallback.heroTitle),
    heroSubtitle: normalizeText(content?.heroSubtitle, fallback.heroSubtitle)
  };
}

function normalizeText(value: string | undefined, fallback: string): string {
  const normalized = value?.trim() ?? '';
  return normalized.length > 0 ? normalized : fallback;
}
