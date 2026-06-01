import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

// ─── Interfaces ─────────────────────────────────────────────────────────────

export interface Season {
  seasonId: number;
  name: string;
  percentageChange: number;
  startDate: string; // ISO date "YYYY-MM-DD"
  endDate: string;
  isActive: boolean;
}

export interface SeasonPayload {
  name: string;
  percentageChange: number;
  startDate: string;
  endDate: string;
}

// ─── Servicio ────────────────────────────────────────────────────────────────

@Injectable({ providedIn: 'root' })
export class SeasonsService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/admin/seasons`;

  getAll(): Observable<Season[]> {
    return this.http.get<Season[]>(this.base);
  }

  create(payload: SeasonPayload): Observable<Season> {
    return this.http.post<Season>(this.base, payload);
  }

  update(id: number, payload: SeasonPayload): Observable<Season> {
    return this.http.put<Season>(`${this.base}/${id}`, payload);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
