import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

// ─── Interfaces ─────────────────────────────────────────────────────────────

export interface RoomTypeImage {
  roomTypeImageId: number;
  roomTypeId: number;
  url: string;
  altText: string;
  displayOrder: number;
}

export interface RoomTypeDetail {
  roomTypeId: number;
  name: string;
  basePrice: number;
  isActive: boolean;
  description: string;
  capacity: number;
  images: RoomTypeImage[];
}

export interface RoomTypePayload {
  name: string;
  basePrice: number;
}

// ─── Servicio ────────────────────────────────────────────────────────────────

@Injectable({ providedIn: 'root' })
export class RoomTypesService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl;

  getAll(): Observable<RoomTypeDetail[]> {
    return this.http.get<RoomTypeDetail[]>(`${this.apiBaseUrl}/admin/room-types`);
  }

  create(payload: RoomTypePayload): Observable<RoomTypeDetail> {
    return this.http.post<RoomTypeDetail>(`${this.apiBaseUrl}/admin/room-types`, payload);
  }

  update(roomTypeId: number, payload: RoomTypePayload): Observable<RoomTypeDetail> {
    return this.http.put<RoomTypeDetail>(`${this.apiBaseUrl}/admin/room-types/${roomTypeId}`, payload);
  }

  delete(roomTypeId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiBaseUrl}/admin/room-types/${roomTypeId}`);
  }
}
