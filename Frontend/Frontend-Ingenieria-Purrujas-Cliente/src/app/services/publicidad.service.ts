import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

export interface Promocion {
  promotionId: number;
  name: string;
  link?: string;
  discount: number;
  startDate: string;
  endDate: string;
  roomTypeId: number;
}

export interface Publicidad {
  advertisingId: number;
  title: string;
  description: string;
  imageUrl: string | null;
  link: string;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class PublicidadService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5234/api';
  
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
    { promotionId: 1, name: 'Escapada Romántica',  link: 'http://localhost:4204/about-us#reservas', discount: 25, startDate: '2026-04-01', endDate: '2026-05-31', roomTypeId: 2 },
    { promotionId: 2, name: 'Semana Ecológica',    link: 'http://localhost:4204/about-us#reservas', discount: 20, startDate: '2026-04-15', endDate: '2026-06-30', roomTypeId: 1 },
    { promotionId: 3, name: 'Aventura Familiar',   link: 'http://localhost:4204/about-us#reservas', discount: 30, startDate: '2026-05-01', endDate: '2026-07-15', roomTypeId: 3 },
    { promotionId: 4, name: 'Retiro de Bienestar', link: 'http://localhost:4204/about-us#reservas', discount: 15, startDate: '2026-06-01', endDate: '2026-08-31', roomTypeId: 2 }
  ];

  private mockPublicidades: Publicidad[] = [
    { advertisingId: 1, title: 'Descuento en Tours al Volcán Irazú', description: 'Recorre el imponente Volcán Irazú con guías expertos y tarifas especiales para huéspedes del hotel.', imageUrl: null, link: 'https://www.costaricaadventuretours.com', isActive: true },
    { advertisingId: 2, title: 'Spa Las Piedras — Tratamientos Termales', description: 'Disfruta de tratamientos termales y masajes relajantes en el exclusivo Spa Las Piedras.', imageUrl: null, link: 'https://www.spalaspiedras.com', isActive: true },
    { advertisingId: 3, title: 'Restaurante La Catarata — Gastronomía Típica', description: 'Sabores auténticos de la cocina costarricense en un ambiente natural inigualable.', imageUrl: null, link: 'https://www.restaurantelacatarata.com', isActive: true },
    { advertisingId: 4, title: 'Birdwatching Turrialba — Avistamiento de Aves', description: 'Más de 400 especies de aves en tours guiados por expertos ornitólogos de la región.', imageUrl: null, link: 'https://www.turrialbabirding.com', isActive: true },
    { advertisingId: 5, title: 'Rafting Río Reventazón — Aventura Extrema', description: 'Vive la adrenalina del rafting en uno de los ríos más emocionantes de Costa Rica.', imageUrl: null, link: 'https://www.riostropicales.com', isActive: true },
    { advertisingId: 6, title: 'Artesanías Cartago — Arte Local', description: 'Lleva un pedazo de la cultura cartaginesa con hermosas artesanías hechas a mano.', imageUrl: null, link: 'https://www.artesanias-cartago.com', isActive: true }
  ];
}
