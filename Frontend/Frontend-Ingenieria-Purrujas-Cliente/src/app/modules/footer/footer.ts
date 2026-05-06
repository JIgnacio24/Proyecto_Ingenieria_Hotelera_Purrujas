import { Component, OnInit, inject, ChangeDetectionStrategy, ChangeDetectorRef, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
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
  styleUrl: './footer.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FooterComponent implements OnInit, AfterViewInit {
  private publicidadService = inject(PublicidadService);
  private cdr = inject(ChangeDetectorRef);

  @ViewChild('tickerTrack') tickerTrack: ElementRef | undefined;

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
        link: '/promociones', // Las promociones siempre van a /promociones
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
      this.cdr.markForCheck(); // Señalar que el componente tiene cambios
      
      // Forzar reinicio de la animación después de que los datos estén listos
      this.restartAnimation();
    });
  }

  ngAfterViewInit(): void {
    // Reiniciar animación cuando la vista esté lista
    this.restartAnimation();
  }

  private restartAnimation(): void {
    // Pequeño delay para asegurar que el DOM está listo
    setTimeout(() => {
      if (this.tickerTrack?.nativeElement) {
        const element = this.tickerTrack.nativeElement;
        // Remover y agregar la animación para reiniciarla
        element.style.animation = 'none';
        setTimeout(() => {
          element.style.animation = '';
        }, 10);
      }
    }, 50);
  }

  private isPromoActive(startDate: string, endDate: string): boolean {
    const hoy = new Date();
    return new Date(startDate) <= hoy && new Date(endDate) >= hoy;
  }

  // Duplicate items for seamless infinite-scroll ticker
  get tickerItems(): CarouselItem[] {
    // Triplicar para asegurar que hay suficiente contenido para la animación infinita
    return [...this.carouselItems, ...this.carouselItems, ...this.carouselItems];
  }
}


