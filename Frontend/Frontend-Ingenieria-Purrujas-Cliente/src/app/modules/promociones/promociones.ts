import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { PublicidadService, Promocion } from '../../services/publicidad.service';
import { ChangeDetectorRef } from '@angular/core';

const ROOM_NAMES: Record<number, string> = {
  1: 'Habitación Doble',
  2: 'Suite Volcán',
  3: 'Villa Familiar'
};

/** Mapea roomTypeId → key que usa ReservarComponent */
const ROOM_ID_MAP: Record<number, string> = {
  1: 'doble',
  2: 'suite',
  3: 'villa'
};

@Component({
  selector: 'app-promociones',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './promociones.html',
  styleUrl: './promociones.css'
})
export class Promociones implements OnInit {
  private publicidadService = inject(PublicidadService);
  private cdr = inject(ChangeDetectorRef);
  private router = inject(Router);

  promociones: Promocion[] = [];
  filtroActivo: 'todas' | 'activas' | 'proximas' = 'todas';

  ngOnInit(): void {
    this.publicidadService.getPromociones().subscribe(data => {
      this.promociones = data;
      this.setFiltro('todas'); // Apply default filter
      this.cdr.detectChanges(); // Force change detection
    });
  }

  setFiltro(filtro: 'todas' | 'activas' | 'proximas'): void {
    this.filtroActivo = filtro;
  }

  get promocionesFiltradas(): Promocion[] {
    const hoy = new Date();
    if (this.filtroActivo === 'activas') {
      return this.promociones.filter(p =>
        new Date(p.startDate) <= hoy && new Date(p.endDate) >= hoy
      );
    }
    if (this.filtroActivo === 'proximas') {
      return this.promociones.filter(p => new Date(p.startDate) > hoy);
    }
    return this.promociones;
  }

  isActiva(promo: Promocion): boolean {
    const hoy = new Date();
    return new Date(promo.startDate) <= hoy && new Date(promo.endDate) >= hoy;
  }

  diasRestantes(endDate: string): number {
    const hoy = new Date();
    const fin = new Date(endDate);
    return Math.max(0, Math.ceil((fin.getTime() - hoy.getTime()) / (1000 * 60 * 60 * 24)));
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('es-CR', { day: 'numeric', month: 'long', year: 'numeric' });
  }

  roomName(roomTypeId: number): string {
    return ROOM_NAMES[roomTypeId] ?? `Tipo ${roomTypeId}`;
  }

  /** Navega a /reservar con fechas, habitación y descuento pre-cargados */
  aprovecharOferta(promo: Promocion): void {
    const habitacion = ROOM_ID_MAP[promo.roomTypeId] ?? 'doble';
    this.router.navigate(['/reservar'], {
      queryParams: {
        inicio: promo.startDate.split('T')[0],
        fin: promo.endDate.split('T')[0],
        habitacion,
        descuento: promo.discount
      }
    });
  }
}
