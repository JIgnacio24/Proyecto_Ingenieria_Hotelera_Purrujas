import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface AboutUsPageContent {
  // Contrato compartido con el endpoint /api/about-us-content.
  // Cada propiedad corresponde a un texto editable de la pagina publica.
  historyTag: string;
  historyTitle: string;
  historyDescription: string;
  historyTimelineStartYear: string;
  historyMilestones: string[];
  historyTimelineEndLabel: string;
  teamTag: string;
  teamTitle: string;
  collaboratorsCount: number;
  localTalentPercentage: number;
  experienceYears: number;
  directorName: string;
  directorTitle: string;
  directorBiography: string;
  philosophyTitle: string;
  philosophyDescription: string;
  philosophyQuote: string;
  mvvTag: string;
  mvvTitle: string;
  missionTitle: string;
  mission: string;
  visionTitle: string;
  vision: string;
  valuesTitle: string;
  values: string[];
  galleryTag: string;
  galleryTitle: string;
  gallerySubtext: string;
}

@Injectable({ providedIn: 'root' })
export class AboutUsContentService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl;

  getContent(): Observable<AboutUsPageContent> {
    return this.http.get<AboutUsPageContent>(`${this.apiBaseUrl}/about-us-content`);
  }

  updateContent(payload: AboutUsPageContent): Observable<AboutUsPageContent> {
    return this.http.put<AboutUsPageContent>(`${this.apiBaseUrl}/about-us-content`, payload);
  }
}

export function createDefaultAboutUsPageContent(): AboutUsPageContent {
  return createEmptyAboutUsPageContent();
}

export function cloneAboutUsPageContent(content: AboutUsPageContent): AboutUsPageContent {
  // Crea copias independientes para evitar mutar la version original al editar el formulario.
  return {
    ...createEmptyAboutUsPageContent(),
    ...content,
    missionTitle: normalizeText(content.missionTitle, 'Mision'),
    visionTitle: normalizeText(content.visionTitle, 'Vision'),
    valuesTitle: normalizeText(content.valuesTitle, 'Valores'),
    historyMilestones: [...(content.historyMilestones ?? [])],
    values: [...(content.values ?? [])]
  };
}

function normalizeText(value: string | undefined, fallback: string): string {
  // Protege la UI si un registro historico aun no tiene campos nuevos en el JSON.
  const normalized = value?.trim() ?? '';
  return normalized.length > 0 ? normalized : fallback;
}

function createEmptyAboutUsPageContent(): AboutUsPageContent {
  return {
    historyTag: '',
    historyTitle: '',
    historyDescription: '',
    historyTimelineStartYear: '',
    historyMilestones: [],
    historyTimelineEndLabel: '',
    teamTag: '',
    teamTitle: '',
    collaboratorsCount: 0,
    localTalentPercentage: 0,
    experienceYears: 0,
    directorName: '',
    directorTitle: '',
    directorBiography: '',
    philosophyTitle: '',
    philosophyDescription: '',
    philosophyQuote: '',
    mvvTag: '',
    mvvTitle: '',
    missionTitle: 'Mision',
    mission: '',
    visionTitle: 'Vision',
    vision: '',
    valuesTitle: 'Valores',
    values: [],
    galleryTag: '',
    galleryTitle: '',
    gallerySubtext: ''
  };
}
