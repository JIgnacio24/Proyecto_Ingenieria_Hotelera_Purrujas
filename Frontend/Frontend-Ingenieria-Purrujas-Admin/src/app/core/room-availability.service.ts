import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map, Observable } from 'rxjs';
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

export interface RoomAvailabilityReportResponse {
  blob: Blob;
  fileName: string;
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

  getTodayReport(): Observable<RoomAvailabilityReportResponse> {
    return this.http
      .get(`${this.apiBaseUrl}/admin/room-availability/today/report`, {
        observe: 'response',
        responseType: 'blob'
      })
      .pipe(
        map((response: HttpResponse<Blob>) => ({
          blob: response.body ?? new Blob([], { type: 'application/pdf' }),
          fileName: this.resolvePdfFileName(response)
        }))
      );
  }

  private resolvePdfFileName(response: HttpResponse<Blob>): string {
    const contentDisposition = response.headers.get('content-disposition') ?? '';
    const fileNameMatch = contentDisposition.match(/filename\*?=(?:UTF-8''|")?([^\";]+)/i);
    const fileName = fileNameMatch?.[1]?.trim();

    if (!fileName) {
      return `reporte_estado_habitaciones_${new Date().toISOString().slice(0, 16).replace('T', '_').replace(':', '-')}.pdf`;
    }

    return decodeURIComponent(fileName).replace(/^"+|"+$/g, '');
  }
}
