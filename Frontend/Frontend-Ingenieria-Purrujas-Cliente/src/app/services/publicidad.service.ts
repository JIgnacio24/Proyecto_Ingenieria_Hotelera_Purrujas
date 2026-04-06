import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

export interface Promocion {
  promotionId: number;
  name: string;
  discount: number;
  startDate: string;
  endDate: string;
  roomTypeId: number;
}

export interface Publicidad {
  advertisingId: number;
  name: string;
  link: string;
}

@Injectable({ providedIn: 'root' })
export class PublicidadService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5232/api';

  getPromociones(): Observable<Promocion[]> {
    return this.http.get<Promocion[]>(`${this.apiUrl}/promotions`).pipe(
      catchError(() => of(this.mockPromociones))
    );
  }

  getPublicidades(): Observable<Publicidad[]> {
    return this.http.get<Publicidad[]>(`${this.apiUrl}/advertising`).pipe(
      catchError(() => of(this.mockPublicidades))
    );
  }

  private mockPromociones: Promocion[] = [
    { promotionId: 1, name: 'Escapada Romántica',  discount: 25, startDate: '2026-04-01', endDate: '2026-05-31', roomTypeId: 2 },
    { promotionId: 2, name: 'Semana Ecológica',    discount: 20, startDate: '2026-04-15', endDate: '2026-06-30', roomTypeId: 1 },
    { promotionId: 3, name: 'Aventura Familiar',   discount: 30, startDate: '2026-05-01', endDate: '2026-07-15', roomTypeId: 3 },
    { promotionId: 4, name: 'Retiro de Bienestar', discount: 15, startDate: '2026-06-01', endDate: '2026-08-31', roomTypeId: 2 }
  ];

  private mockPublicidades: Publicidad[] = [
    { advertisingId: 1, name: 'Descuento en Tours al Volcán Irazú',          link: 'https://www.costaricaadventuretours.com' },
    { advertisingId: 2, name: 'Spa Las Piedras — Tratamientos Termales',     link: 'https://www.spalaspiedras.com' },
    { advertisingId: 3, name: 'Restaurante La Catarata — Gastronomía Típica', link: 'https://www.restaurantelacatarata.com' },
    { advertisingId: 4, name: 'Birdwatching Turrialba — Avistamiento de Aves', link: 'https://www.turrialbabirding.com' },
    { advertisingId: 5, name: 'Rafting Río Reventazón — Aventura Extrema',   link: 'https://www.riostropicales.com' },
    { advertisingId: 6, name: 'Artesanías Cartago — Arte Local',             link: 'https://www.artesanias-cartago.com' }
  ];
}
