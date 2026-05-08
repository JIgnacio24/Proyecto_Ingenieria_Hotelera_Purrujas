import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  createDefaultFacilitiesPageContent,
  FacilitiesContentService,
  FacilitiesPageContent
} from '../../services/facilities-content.service';
import { GalleryImage, GalleryImagesService } from '../../services/galleryImages.service';


type FacilityServiceViewModel = {
  title: string;
  description: string;
  imageUrl: string;
  imageAlt: string;
  animationClass: string;
};

const FACILITY_SERVICE_MEDIA = [
  { imageName: 'habitacion_doble_2.png', animationClass: '' },
  { imageName: 'restaurante_la_ceiba.png', animationClass: 'delay-1' },
  { imageName: 'piscinas_naturales.png', animationClass: 'delay-2' },
  { imageName: 'senderos.png', animationClass: 'delay-3' },
  { imageName: 'salon_eventos.png', animationClass: '' },
  { imageName: 'spa.png', animationClass: 'delay-1' },
  { imageName: 'senderismo_volcan.png', animationClass: 'delay-2' },
  { imageName: 'avistamiento_aves.png', animationClass: 'delay-3' },
  { imageName: 'gastronomia_tipica.png', animationClass: '' },
  { imageName: 'transporte.png', animationClass: 'delay-1' },
  { imageName: 'internet.png', animationClass: 'delay-2' },
  { imageName: 'atencion_personalizada.png', animationClass: 'delay-3' }
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
  private readonly galleryImagesService = inject(GalleryImagesService);

  readonly content = signal<FacilitiesPageContent>(createDefaultFacilitiesPageContent());
  readonly galleryImages = signal<GalleryImage[]>([]);
  readonly serviceCards = computed<FacilityServiceViewModel[]>(() => {
    const images = this.galleryImages();

    return this.content()
      .serviceCards.slice(0, FACILITY_SERVICE_MEDIA.length)
      .map((service, index) => {
        const media = FACILITY_SERVICE_MEDIA[index];
        const image = images.find((item) => item.name === media.imageName);

        return {
          title: service.title,
          description: service.description,
          imageUrl: image ? `http://localhost:5232${image.src}` : '',
          imageAlt: image?.alt ?? service.title,
          animationClass: media.animationClass
        };
      });
  });

  constructor() {
    void this.loadContent();
    void this.loadGalleryImages();
  }

  private async loadContent(): Promise<void> {
    const content = await firstValueFrom(this.facilitiesContentService.getContent());
    this.content.set(content);
  }

  private async loadGalleryImages(): Promise<void> {
    const images = await firstValueFrom(this.galleryImagesService.getAll());
    this.galleryImages.set(images);
  }
}
