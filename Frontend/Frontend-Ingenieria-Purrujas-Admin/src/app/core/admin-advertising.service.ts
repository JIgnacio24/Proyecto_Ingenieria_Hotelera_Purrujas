import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface AdminAdvertising {
  advertisingId: number;
  title: string;
  description: string;
  imageUrl: string | null;
  link: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface AdvertisingPayload {
  title: string;
  description: string;
  imageUrl: string | null;
  link: string;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class AdminAdvertisingService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/admin/advertising`;

  getAll(): Observable<AdminAdvertising[]> {
    return this.http.get<AdminAdvertising[]>(this.base);
  }

  create(payload: AdvertisingPayload): Observable<AdminAdvertising> {
    return this.http.post<AdminAdvertising>(this.base, payload);
  }

  update(id: number, payload: AdvertisingPayload): Observable<AdminAdvertising> {
    return this.http.put<AdminAdvertising>(`${this.base}/${id}`, payload);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
