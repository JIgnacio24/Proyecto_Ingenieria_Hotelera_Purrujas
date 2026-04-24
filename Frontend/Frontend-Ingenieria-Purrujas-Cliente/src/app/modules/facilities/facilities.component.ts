import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  createDefaultFacilitiesPageContent,
  FacilitiesContentService,
  FacilitiesPageContent
} from '../../services/facilities-content.service';

type FacilityServiceViewModel = {
  title: string;
  description: string;
  imageUrl: string;
  imageAlt: string;
  animationClass: string;
};

const FACILITY_SERVICE_MEDIA = [
  {
    imageUrl: '/images/habitacion_doble_2.png',
    imageAlt: 'Habitaciones tematicas con vista al bosque',
    animationClass: ''
  },
  {
    imageUrl: '/images/restaurante_la_ceiba.png',
    imageAlt: 'Restaurante La Ceiba con ingredientes de finca',
    animationClass: 'delay-1'
  },
  {
    imageUrl: '/images/piscinas_naturales.png',
    imageAlt: 'Piscina natural de manantial',
    animationClass: 'delay-2'
  },
  {
    imageUrl: '/images/senderos.png',
    imageAlt: 'Senderos ecologicos privados',
    animationClass: 'delay-3'
  },
  {
    imageUrl: '/images/salon_eventos.png',
    imageAlt: 'Salon de eventos rodeado de naturaleza',
    animationClass: ''
  },
  {
    imageUrl: '/images/spa.png',
    imageAlt: 'Spa con plantas locales',
    animationClass: 'delay-1'
  },
  {
    imageUrl: '/images/senderismo_volcan.png',
    imageAlt: 'Tour al Volcan Turrialba e Irazu',
    animationClass: 'delay-2'
  },
  {
    imageUrl: '/images/avistamiento_aves.png',
    imageAlt: 'Birdwatching con guias certificados',
    animationClass: 'delay-3'
  },
  {
    imageUrl: '/images/gastronomia_tipica.png',
    imageAlt: 'Talleres de gastronomia tipica',
    animationClass: ''
  },
  {
    imageUrl: '/images/transporte.png',
    imageAlt: 'Transporte privado desde San Jose',
    animationClass: 'delay-1'
  },
  {
    imageUrl: '/images/internet.png',
    imageAlt: 'Wi-Fi de alta velocidad en el hotel',
    animationClass: 'delay-2'
  },
  {
    imageUrl: '/images/atencion_personalizada.png',
    imageAlt: 'Atencion personalizada todo el dia',
    animationClass: 'delay-3'
  }
] as const;

@Component({
  selector: 'app-facilities',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './facilities.component.html',
  styleUrl: './facilities.component.css'
})
export class FacilitiesComponent {
  private readonly facilitiesContentService = inject(FacilitiesContentService);

  readonly content = signal<FacilitiesPageContent>(createDefaultFacilitiesPageContent());
  readonly serviceCards = computed<FacilityServiceViewModel[]>(() =>
    this.content()
      .serviceCards.slice(0, FACILITY_SERVICE_MEDIA.length)
      .map((service, index) => ({
        title: service.title,
        description: service.description,
        imageUrl: FACILITY_SERVICE_MEDIA[index].imageUrl,
        imageAlt: FACILITY_SERVICE_MEDIA[index].imageAlt,
        animationClass: FACILITY_SERVICE_MEDIA[index].animationClass
      }))
  );

  constructor() {
    void this.loadContent();
  }

  private async loadContent(): Promise<void> {
    const content = await firstValueFrom(this.facilitiesContentService.getContent());
    this.content.set(content);
  }
}
