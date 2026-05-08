import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface RoomTypeOption {
  roomTypeId: number;
  name: string;
}

export interface RoomStatusTodayItem {
  roomId: number;
  roomNumber: string;
  roomTypeName: string;
  statusName: string;
  isAvailable: boolean;
  currentGuest?: string | null;
}

export interface RoomAvailabilitySummary {
  date: string;
  totalRooms: number;
  availableRooms: number;
  occupiedRooms: number;
  outOfServiceRooms: number;
  rooms: RoomStatusTodayItem[];
  roomTypes: RoomTypeOption[];
}

export interface RoomAvailabilityItem {
  roomId: number;
  roomNumber: string;
  roomTypeName: string;
  operationalStatus: string;
  basePricePerNight: number;
  nightsTotal: number;
  highSeasonNights: number;
  lowSeasonNights: number;
  highestSeasonMultiplier: number;
  totalUsd: number;
}

export interface RoomAvailabilitySearchResult {
  startDate: string;
  endDate: string;
  roomTypeId?: number | null;
  roomTypeName: string;
  availableRooms: number;
  rooms: RoomAvailabilityItem[];
}

@Injectable({ providedIn: 'root' })
export class RoomAvailabilityService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl;

  getToday(): Observable<RoomAvailabilitySummary> {
    return this.http.get<RoomAvailabilitySummary>(`${this.apiBaseUrl}/admin/room-availability/today`);
  }

  search(
    startDate: string,
    endDate: string,
    roomTypeId: number | null
  ): Observable<RoomAvailabilitySearchResult> {
    let params = new HttpParams()
      .set('startDate', startDate)
      .set('endDate', endDate);

    if (roomTypeId) {
      params = params.set('roomTypeId', roomTypeId);
    }

    return this.http.get<RoomAvailabilitySearchResult>(
      `${this.apiBaseUrl}/admin/room-availability/search`,
      { params }
    );
  }
}
