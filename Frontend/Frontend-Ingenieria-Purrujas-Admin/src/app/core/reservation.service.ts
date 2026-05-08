import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface AdminReservationResponse {
  reservationId: number;
  roomNumber: string;
  roomTypeName: string;
  customerFullName: string;
  customerEmail: string;
  startDate: string;
  endDate: string;
  nightsTotal: number;
  nightsHigh: number;
  nightsLow: number;
  totalUsd: number;
  totalCrc: number;
  currency: string;
  status: string;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class ReservationService {
  private readonly apiBase = environment.apiBaseUrl;

  constructor(private http: HttpClient) {}

  getAll(): Observable<AdminReservationResponse[]> {
    return this.http.get<AdminReservationResponse[]>(`${this.apiBase}/reservation`);
  }

  updateStatus(id: number, status: string): Observable<void> {
    return this.http.patch<void>(`${this.apiBase}/reservation/${id}/status`, { status });
  }
}
