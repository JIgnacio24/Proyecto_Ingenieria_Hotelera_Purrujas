import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

// ─── Interfaces ─────────────────────────────────────────────────────────────

export interface OccupancyPrediction {
  year: number;
  month: number;
  occupancyPercentage: number;
}

// ─── Servicio ────────────────────────────────────────────────────────────────

@Injectable({ providedIn: 'root' })
export class PredictionService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/predictions`;

  getPredictions(): Observable<OccupancyPrediction[]> {
    return this.http.get<OccupancyPrediction[]>(`${this.base}/occupancy`);
  }

  getHistory(): Observable<OccupancyPrediction[]> {
    return this.http.get<OccupancyPrediction[]>(`${this.base}/history`);
  }
}
