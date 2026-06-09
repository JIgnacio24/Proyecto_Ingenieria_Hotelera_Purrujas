import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface RoomDetail {
  roomId: number;
  roomNumber: string;
  isActive: boolean;
  roomTypeId: number;
  roomTypeName: string;
  roomStatusId: number;
  roomStatusName: string;
}

export interface RoomStatusOption {
  roomStatusId: number;
  name: string;
  description: string;
  isAvailableForBooking: boolean;
}

export interface RoomPayload {
  roomNumber: string;
  roomTypeId: number;
  roomStatusId: number;
}

@Injectable({ providedIn: 'root' })
export class RoomsService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl;

  getAll(): Observable<RoomDetail[]> {
    return this.http.get<RoomDetail[]>(`${this.apiBaseUrl}/admin/rooms`);
  }

  getById(roomId: number): Observable<RoomDetail> {
    return this.http.get<RoomDetail>(`${this.apiBaseUrl}/admin/rooms/${roomId}`);
  }

  getStatuses(): Observable<RoomStatusOption[]> {
    return this.http.get<RoomStatusOption[]>(`${this.apiBaseUrl}/admin/rooms/statuses`);
  }

  create(payload: RoomPayload): Observable<RoomDetail> {
    return this.http.post<RoomDetail>(`${this.apiBaseUrl}/admin/rooms`, payload);
  }

  update(roomId: number, payload: RoomPayload): Observable<RoomDetail> {
    return this.http.put<RoomDetail>(`${this.apiBaseUrl}/admin/rooms/${roomId}`, payload);
  }

  delete(roomId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiBaseUrl}/admin/rooms/${roomId}`);
  }
}
