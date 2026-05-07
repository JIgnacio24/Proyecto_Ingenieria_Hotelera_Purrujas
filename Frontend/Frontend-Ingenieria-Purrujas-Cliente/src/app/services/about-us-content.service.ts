import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface AboutUsPageContent {
  // Contrato recibido desde la base de datos por medio del endpoint publico.
  // La vista cliente no debe depender de textos quemados para estas secciones.
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
  private readonly apiUrl = '/api';

  getContent(): Observable<AboutUsPageContent> {
    return this.http
      .get<Partial<AboutUsPageContent>>(`${this.apiUrl}/about-us-content`)
      .pipe(map((content) => normalizeAboutUsPageContent(content)));
  }
}

export function createEmptyAboutUsPageContent(): AboutUsPageContent {
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

export function normalizeAboutUsPageContent(
  content: Partial<AboutUsPageContent> | null | undefined
): AboutUsPageContent {
  // Tolera registros anteriores del JSON mientras la migracion agrega los campos nuevos.
  const empty = createEmptyAboutUsPageContent();
  const normalizedContent = content ?? {};

  return {
    ...empty,
    ...normalizedContent,
    missionTitle: normalizeText(normalizedContent.missionTitle, empty.missionTitle),
    visionTitle: normalizeText(normalizedContent.visionTitle, empty.visionTitle),
    valuesTitle: normalizeText(normalizedContent.valuesTitle, empty.valuesTitle),
    historyMilestones: normalizeList(normalizedContent.historyMilestones),
    values: normalizeList(normalizedContent.values)
  };
}

function normalizeText(value: string | undefined, fallback: string): string {
  // Evita que una cadena vacia deje invisible un titulo editable.
  const normalized = value?.trim() ?? '';
  return normalized.length > 0 ? normalized : fallback;
}

function normalizeList(values: string[] | undefined): string[] {
  return (values ?? [])
    .map((value) => value?.trim() ?? '')
    .filter((value) => value.length > 0);
}
