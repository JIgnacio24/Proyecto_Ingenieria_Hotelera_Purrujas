import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface RoomType {
  roomTypeId: number;
  name: string;
  basePrice: number;
  isActive: boolean;
}

export interface RoomTypePayload {
  name: string;
  basePrice: number;
}

@Injectable({ providedIn: 'root' })
export class RoomTypesService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl;

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
  }
}
