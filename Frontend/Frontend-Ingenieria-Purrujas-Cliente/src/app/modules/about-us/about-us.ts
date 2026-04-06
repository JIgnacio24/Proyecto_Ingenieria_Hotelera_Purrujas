import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { FacilitiesComponent } from '../facilities/facilities.component';
import { Currency, CurrencyService } from '../../shared/currency.service';
import { Subscription } from 'rxjs';

interface GalleryItem {
  src: string;
  alt: string;
  caption: string;
  category: 'hotel' | 'lugares';
}
@Component({
  selector: 'app-about-us',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, FacilitiesComponent],
  templateUrl: './about-us.html',
  styleUrl: './about-us.css'
})

export class AboutUs implements OnInit, OnDestroy {
  activeFilter: 'todos' | 'hotel' | 'lugares' = 'todos';
  currency: Currency = 'USD';
  currencySymbol = '$';
  private subs = new Subscription();

  galleryItems: GalleryItem[] = [
    {
      src: '/images/foto_fondo.png',
      alt: 'Hotel Las Purrujas',
      caption: 'Hotel Las Purrujas',
      category: 'hotel'
    },
    {
      src: '/images/habitación_doble.png',
      alt: 'Habitación doble',
      caption: 'Habitación doble',
      category: 'hotel'
    },
    {
      src: '/images/habitacion_doble_2.png',
      alt: 'Habitación doble con vista',
      caption: 'Habitación doble · vista balcón',
      category: 'hotel'
    },
    {
      src: '/images/piscinas_naturales.png',
      alt: 'Piscinas naturales del hotel',
      caption: 'Piscinas naturales',
      category: 'hotel'
    },
    {
      src: '/images/restaurante_la_ceiba.png',
      alt: 'Restaurante La Ceiba',
      caption: 'Restaurante La Ceiba',
      category: 'hotel'
    },
    {
      src: '/images/spa.png',
      alt: 'Spa y bienestar',
      caption: 'Spa y bienestar',
      category: 'hotel'
    },
    {
      src: '/images/vista_balcon.png',
      alt: 'Vista desde el balcón',
      caption: 'Vista desde el balcón',
      category: 'hotel'
    },
    {
      src: '/images/villa_familiar.png',
      alt: 'Villa familiar',
      caption: 'Villa familiar',
      category: 'hotel'
    },
    {
      src: '/images/gastronomia_tipica.png',
      alt: 'Gastronomía típica',
      caption: 'Gastronomía típica',
      category: 'hotel'
    },
    {
      src: '/images/avistamiento_aves.png',
      alt: 'Avistamiento de aves en los alrededores',
      caption: 'Avistamiento de aves',
      category: 'lugares'
    },
    {
      src: '/images/senderismo_volcan.png',
      alt: 'Senderismo en el volcán Turrialba',
      caption: 'Senderismo en el volcán',
      category: 'lugares'
    },
    {
      src: '/images/senderos.png',
      alt: 'Senderos ecológicos de la zona',
      caption: 'Senderos ecológicos',
      category: 'lugares'
    },
  ];

  get filteredItems(): GalleryItem[] {
    if (this.activeFilter === 'todos') return this.galleryItems;
    return this.galleryItems.filter(item => item.category === this.activeFilter);
  }

  constructor(public currencyService: CurrencyService) {}

  ngOnInit(): void {
    this.subs.add(
      this.currencyService.currencyChanges$.subscribe(curr => {
        this.currency = curr;
        this.currencySymbol = this.currencyService.symbol(curr);
      })
    );
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
  }

  price(amountUsd: number): number {
    return this.currencyService.convertFromUsd(amountUsd, this.currency);
  }

  trackBySrc(_index: number, item: GalleryItem): string {
    return item.src;
  }

  setFilter(filter: 'todos' | 'hotel' | 'lugares'): void {
    this.activeFilter = filter;
  }
}
