import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { PublicidadService, Promocion, Publicidad } from '../../services/publicidad.service';
import { combineLatest } from 'rxjs';

interface CarouselItem {
  id: string;
  type: 'promocion' | 'publicidad';
  name: string;
  link: string;
  discount?: number;
  isActive?: boolean;
}

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './footer.html',
  styleUrl: './footer.css'
})
export class FooterComponent implements OnInit {
  private publicidadService = inject(PublicidadService);

  carouselItems: CarouselItem[] = [];

  ngOnInit(): void {
    // Usar combineLatest para esperar a que AMBAS llamadas terminen
    combineLatest([
      this.publicidadService.getPromociones(),
      this.publicidadService.getPublicidades()
    ]).subscribe(([promociones, publicidades]) => {
      // Convertir promociones a items del carrusel
      const promoItems: CarouselItem[] = promociones.map(p => ({
        id: `promo-${p.promotionId}`,
        type: 'promocion',
        name: p.name,
        link: p.link || '/promociones',
        discount: p.discount,
        isActive: this.isPromoActive(p.startDate, p.endDate)
      }));

      // Convertir publicidades a items del carrusel
      const pubItems: CarouselItem[] = publicidades.map(pub => ({
        id: `pub-${pub.advertisingId}`,
        type: 'publicidad',
        name: pub.name,
        link: pub.link
      }));

      // Mezclar ambas listas
      this.carouselItems = [...promoItems, ...pubItems];
    });
  }

  private isPromoActive(startDate: string, endDate: string): boolean {
    const hoy = new Date();
    return new Date(startDate) <= hoy && new Date(endDate) >= hoy;
  }

  // Duplicate items for seamless infinite-scroll ticker
  get tickerItems(): CarouselItem[] {
    return [...this.carouselItems, ...this.carouselItems];
  }
}


