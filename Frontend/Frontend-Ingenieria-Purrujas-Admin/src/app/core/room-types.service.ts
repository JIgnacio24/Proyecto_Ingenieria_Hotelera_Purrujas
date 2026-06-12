import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map, Observable } from 'rxjs';
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
  description?: string | null;
  capacity?: number;
  images?: RoomTypeImage[];
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
    return this.http.get<RoomTypeDetail[]>(`${this.apiBaseUrl}/admin/room-types`).pipe(
      map((roomTypes) => {
        const valid = roomTypes.filter(rt => rt.name?.trim().length > 0);
        return this.dedupeRoomTypes(valid);
      })
    );
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

  private dedupeRoomTypes(roomTypes: RoomTypeDetail[]): RoomTypeDetail[] {
    const normalized = new Map<string, RoomTypeDetail>();

    for (const roomType of roomTypes) {
      const key = this.normalizeRoomTypeKey(roomType.name);
      const existing = normalized.get(key);

      if (!existing || this.prefersRoomType(roomType, existing)) {
        normalized.set(key, roomType);
      }
    }

    return [...normalized.values()].sort((a, b) => a.name.localeCompare(b.name, 'es', { sensitivity: 'base' }));
  }

  private prefersRoomType(candidate: RoomTypeDetail, current: RoomTypeDetail): boolean {
    const candidateHasAccents = candidate.name.trim() !== this.stripDiacritics(candidate.name);
    const currentHasAccents = current.name.trim() !== this.stripDiacritics(current.name);

    if (candidateHasAccents !== currentHasAccents) {
      return candidateHasAccents;
    }

    return candidate.roomTypeId < current.roomTypeId;
  }

  private normalizeRoomTypeKey(value: string): string {
    return this.stripDiacritics(value).toLocaleLowerCase('en-US');
  }

  private stripDiacritics(value: string): string {
    return value
      .trim()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '');
  }
}
