import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, shareReplay, catchError, throwError } from 'rxjs';

export interface PublicRoomType {
  roomTypeId: number;
  name: string;
  basePrice: number;
  description: string | null;
  capacity: number;
}

@Injectable({ providedIn: 'root' })
export class RoomTypesService {
  private readonly http = inject(HttpClient);
  private cache$: Observable<PublicRoomType[]> | null = null;

  getAll(): Observable<PublicRoomType[]> {
    if (!this.cache$) {
      this.cache$ = this.http.get<PublicRoomType[]>('/api/room-types').pipe(
        shareReplay({ bufferSize: 1, refCount: false }),
        catchError((err) => {
          this.cache$ = null;
          return throwError(() => err);
        })
      );
    }
    return this.cache$;
  }
}
