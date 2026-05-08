import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface AvailabilityResponse {
  isAvailable: boolean;
  availableRooms: number;
  roomTypeName: string;
  startDate: string;
  endDate: string;
}

export interface CreateReservationRequest {
  roomTypeKey: string;
  startDate: string;
  endDate: string;
  currency: string;
  name: string;
  lastName: string;
  email: string;
  phone?: string;
  creditCard: string;
}

export interface ReservationResponse {
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
  private readonly apiBase = '/api';

  constructor(private http: HttpClient) {}

  checkAvailability(roomTypeKey: string, startDate: string, endDate: string): Observable<AvailabilityResponse> {
    const params = new HttpParams()
      .set('roomTypeKey', roomTypeKey)
      .set('startDate', startDate)
      .set('endDate', endDate);
    return this.http.get<AvailabilityResponse>(`${this.apiBase}/reservation/availability`, { params });
  }

  createReservation(request: CreateReservationRequest): Observable<ReservationResponse> {
    return this.http.post<ReservationResponse>(`${this.apiBase}/reservation`, request);
  }
}
