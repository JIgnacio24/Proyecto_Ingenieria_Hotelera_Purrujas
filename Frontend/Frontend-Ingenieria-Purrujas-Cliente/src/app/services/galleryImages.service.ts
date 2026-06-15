import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface GalleryImage {
  id: number;
  name: string;
  src: string;
  alt: string;
  caption: string;
  category: 'hotel' | 'lugares' | 'fondo';
  isActive: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class GalleryImagesService {
  private readonly apiUrl = '/api/gallery-images';

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<GalleryImage[]> {
    return this.http.get<GalleryImage[]>(this.apiUrl);
  }
}
