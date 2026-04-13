import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { PublicidadService, Promocion } from '../../services/publicidad.service';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './footer.html',
  styleUrl: './footer.css'
})
export class FooterComponent implements OnInit {
  private publicidadService = inject(PublicidadService);
  promociones: Promocion[] = [];

  ngOnInit(): void {
    this.publicidadService.getPromociones().subscribe(data => {
      this.promociones = data;
    });
  }

  // Duplicated list for seamless infinite-scroll ticker
  get tickerItems(): Promocion[] {
    return [...this.promociones, ...this.promociones];
  }

  isActiva(promo: Promocion): boolean {
    const hoy = new Date();
    return new Date(promo.startDate) <= hoy && new Date(promo.endDate) >= hoy;
  }
}
