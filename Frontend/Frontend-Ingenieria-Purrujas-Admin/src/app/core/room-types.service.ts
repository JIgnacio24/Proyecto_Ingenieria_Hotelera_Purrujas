import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

<<<<<<< Updated upstream
export interface RoomType {
=======
// ─── Interfaces ─────────────────────────────────────────────────────────────

export interface RoomTypeImage {
  roomTypeImageId: number;
  roomTypeId: number;
  url: string;
  altText: string;
  displayOrder: number;
}

export interface RoomTypeDetail {
>>>>>>> Stashed changes
  roomTypeId: number;
  name: string;
  basePrice: number;
  isActive: boolean;
<<<<<<< Updated upstream
}

export interface RoomTypePayload {
  name: string;
  basePrice: number;
}
=======
  description: string;
  capacity: number;
  images: RoomTypeImage[];
}

// ─── Servicio ────────────────────────────────────────────────────────────────
>>>>>>> Stashed changes

@Injectable({ providedIn: 'root' })
export class RoomTypesService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl;

<<<<<<< Updated upstream
  getAll(): Observable<RoomType[]> {
    return this.http.get<RoomType[]>(`${this.apiBaseUrl}/admin/room-types`);
  }

  create(payload: RoomTypePayload): Observable<RoomType> {
    return this.http.post<RoomType>(`${this.apiBaseUrl}/admin/room-types`, payload);
  }

  update(roomTypeId: number, payload: RoomTypePayload): Observable<RoomType> {
    return this.http.put<RoomType>(`${this.apiBaseUrl}/admin/room-types/${roomTypeId}`, payload);
  }

  delete(roomTypeId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiBaseUrl}/admin/room-types/${roomTypeId}`);
=======
  /**
   * Retorna todos los tipos de habitación con descripción, capacidad e imágenes.
   * Requiere token JWT de administrador (interceptor lo agrega automáticamente).
   */
  getAll(): Observable<RoomTypeDetail[]> {
    return this.http.get<RoomTypeDetail[]>(`${this.apiBaseUrl}/admin/room-types`);
>>>>>>> Stashed changes
  }
}
