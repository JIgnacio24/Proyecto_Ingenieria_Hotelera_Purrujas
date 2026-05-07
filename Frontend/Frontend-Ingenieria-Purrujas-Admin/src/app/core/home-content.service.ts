import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface HomePageContent {
  // Contrato compartido con /api/home-content para el hero editable del inicio.
  heroEyebrow: string;
  heroTitle: string;
  heroSubtitle: string;
}

@Injectable({ providedIn: 'root' })
export class HomeContentService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl;

  getContent(): Observable<HomePageContent> {
    return this.http.get<HomePageContent>(`${this.apiBaseUrl}/home-content`);
  }

  updateContent(payload: HomePageContent): Observable<HomePageContent> {
    return this.http.put<HomePageContent>(`${this.apiBaseUrl}/home-content`, payload);
  }
}

export function createDefaultHomePageContent(): HomePageContent {
  return {
    heroEyebrow: 'Bienvenido',
    heroTitle: 'Hotel Las Purrujas',
    heroSubtitle: 'Donde la naturaleza te abraza y Costa Rica te enamora.'
  };
}

export function cloneHomePageContent(content: HomePageContent): HomePageContent {
  return {
    heroEyebrow: normalizeText(content.heroEyebrow, 'Bienvenido'),
    heroTitle: normalizeText(content.heroTitle, 'Hotel Las Purrujas'),
    heroSubtitle: normalizeText(
      content.heroSubtitle,
      'Donde la naturaleza te abraza y Costa Rica te enamora.'
    )
  };
}

function normalizeText(value: string | undefined, fallback: string): string {
  const normalized = value?.trim() ?? '';
  return normalized.length > 0 ? normalized : fallback;
}
