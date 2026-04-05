import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

interface GalleryItem {
  src: string;
  alt: string;
  caption: string;
  category: 'hotel' | 'lugares';
}

@Component({
  selector: 'app-about-us',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './about-us.html',
  styleUrl: './about-us.css'
})
export class AboutUs {
  activeFilter: 'todos' | 'hotel' | 'lugares' = 'todos';

  galleryItems: GalleryItem[] = [
    {
      src: '../scr/Hotel-Las-Purrujas.jpeg',
      alt: 'Hotel Las Purrujas',
      caption: 'Hotel Las Purrujas',
      category: 'hotel'
    },
    {
      src: '../scr/Hotel-Las-Purrujas-2.jpeg',
      alt: 'Hotel Las Purrujas',
      caption: 'Hotel Las Purrujas',
      category: 'hotel'
    },
    {
      src: '../scr/Hotel-Las-Purrujas-Piscinas.jpeg',
      alt: 'Piscinas del Hotel Las Purrujas',
      caption: 'Piscinas',
      category: 'hotel'
    },
    {
      src: '../scr/Hotel-Las-Purrujas-Restaurante.jpeg',
      alt: 'Restaurante del Hotel Las Purrujas',
      caption: 'Restaurante',
      category: 'hotel'
    },
    {
      src: '../scr/Hotel-Las-Purrujas-Habitacion.jpeg',
      alt: 'Habitación del Hotel Las Purrujas',
      caption: 'Habitaciones',
      category: 'hotel'
    },
    {
      src: '../scr/Hotel-Las-Purrujas-Spa.jpeg',
      alt: 'Spa del Hotel Las Purrujas',
      caption: 'Spa',
      category: 'hotel'
    },
    {
      src: '../scr/Hotel-Las-Purrujas-Balcon.jpeg',
      alt: 'Balcón del Hotel Las Purrujas',
      caption: 'Balcón',
      category: 'hotel'
    },
    {
      src: '../scr/Hotel-Las-Purrujas-villa.jpeg',
      alt: 'Villa del Hotel Las Purrujas',
      caption: 'Villa',
      category: 'hotel'
    },
    {
      src: '../scr/Hotel-Las-Purrujas-Desayuno.jpeg',
      alt: 'Desayuno en Hotel Las Purrujas',
      caption: 'Desayuno',
      category: 'hotel'
    },
    {
      src: '../scr/Hotel-Las-Purrujas-aves.jpeg',
      alt: 'Avistamiento de aves en los alrededores',
      caption: 'Avistamiento de Aves',
      category: 'lugares'
    },
    {
      src: '../scr/Hotel-Las-Purrujas-Senderos.jpeg',
      alt: 'Senderos ecológicos de la zona',
      caption: 'Senderos Ecológicos',
      category: 'lugares'
    },
  ];

  get filteredItems(): GalleryItem[] {
    if (this.activeFilter === 'todos') return this.galleryItems;
    return this.galleryItems.filter(item => item.category === this.activeFilter);
  }

  setFilter(filter: 'todos' | 'hotel' | 'lugares'): void {
    this.activeFilter = filter;
  }
}
