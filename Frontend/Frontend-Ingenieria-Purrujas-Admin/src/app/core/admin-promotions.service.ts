import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

// ─── Interfaces ─────────────────────────────────────────────────────────────

export interface AdminPromotion {
  promotionId: number;
  name: string;
  discount: number;
  startDate: string; // ISO date "YYYY-MM-DD"
  endDate: string;
  roomTypeId: number;
  roomTypeName: string;
  isActive: boolean;
}

export interface PromotionPayload {
  name: string;
  discount: number;
  startDate: string;
  endDate: string;
  roomTypeId: number;
}

// ─── Servicio ────────────────────────────────────────────────────────────────

@Injectable({ providedIn: 'root' })
export class AdminPromotionsService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/admin/promotions`;

  getAll(): Observable<AdminPromotion[]> {
    return this.http.get<AdminPromotion[]>(this.base);
  }

  create(payload: PromotionPayload): Observable<AdminPromotion> {
    return this.http.post<AdminPromotion>(this.base, payload);
  }

  update(id: number, payload: PromotionPayload): Observable<AdminPromotion> {
    return this.http.put<AdminPromotion>(`${this.base}/${id}`, payload);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
