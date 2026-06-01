import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface AdminReservationResponse {
  reservationId: number;
  roomId: number;
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

export interface RoomAvailabilityItem {
  roomId: number;
  roomNumber: string;
  roomTypeName: string;
  basePricePerNight: number;
  nightsTotal: number;
  highSeasonNights: number;
  lowSeasonNights: number;
  totalUsd: number;
}

export interface UpdateReservationRequest {
  startDate: string;
  endDate: string;
  roomId: number;
}

@Injectable({ providedIn: 'root' })
export class ReservationService {
  private readonly apiBase = environment.apiBaseUrl;

  constructor(private http: HttpClient) {}

  getAll(): Observable<AdminReservationResponse[]> {
    return this.http.get<AdminReservationResponse[]>(`${this.apiBase}/reservation`);
  }

  getDeleted(): Observable<AdminReservationResponse[]> {
    return this.http.get<AdminReservationResponse[]>(`${this.apiBase}/reservation/deleted`);
  }

  updateStatus(id: number, status: string): Observable<void> {
    return this.http.patch<void>(`${this.apiBase}/reservation/${id}/status`, { status });
  }

  softDelete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiBase}/reservation/${id}`);
  }

  searchAvailableRooms(startDate: string, endDate: string, excludeReservationId?: number): Observable<{ rooms: RoomAvailabilityItem[] }> {
    const params: Record<string, string | number> = { startDate, endDate };
    if (excludeReservationId !== undefined) {
      params['excludeReservationId'] = excludeReservationId;
    }
    return this.http.get<{ rooms: RoomAvailabilityItem[] }>(
      `${this.apiBase}/admin/room-availability/search`,
      { params }
    );
  }

  updateReservation(id: number, data: UpdateReservationRequest): Observable<AdminReservationResponse> {
    return this.http.put<AdminReservationResponse>(`${this.apiBase}/reservation/${id}`, data);
  }
}
