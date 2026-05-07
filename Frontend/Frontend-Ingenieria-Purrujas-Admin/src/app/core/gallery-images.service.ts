import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface GalleryImage {
  id: number;
  name: string;
  src: string;
  alt: string;
  caption: string;
  category: 'hotel' | 'lugares' | 'fondo';
  isActive: boolean;
}

export interface GalleryImageUpdate {
  name: string;
  alt: string;
  caption: string;
  category: 'hotel' | 'lugares' | 'fondo';
  file?: File | null;
}

@Injectable({ providedIn: 'root' })
export class GalleryImagesService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl;

  getAll(): Observable<GalleryImage[]> {
    return this.http.get<GalleryImage[]>(`${this.apiBaseUrl}/gallery-images`);
  }

  update(id: number, payload: GalleryImageUpdate): Observable<GalleryImage> {
    const formData = new FormData();
    formData.append('name', payload.name);
    formData.append('alt', payload.alt);
    formData.append('caption', payload.caption);
    formData.append('category', payload.category);

    if (payload.file) {
      formData.append('file', payload.file);
    }

    return this.http.put<GalleryImage>(`${this.apiBaseUrl}/gallery-images/${id}`, formData);
  }
}
