import { ChangeDetectorRef, Component, NgZone, OnDestroy, OnInit, Pipe, PipeTransform } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { Currency, CurrencyService } from '../../shared/currency.service';
import { Subscription, firstValueFrom, timer } from 'rxjs';
import {
  AboutUsContentService,
  AboutUsPageContent,
  createEmptyAboutUsPageContent,
  normalizeAboutUsPageContent
} from '../../services/about-us-content.service';

interface GalleryItem {
  src: string;
  alt: string;
  caption: string;
  category: 'hotel' | 'lugares';
}

@Pipe({
  name: 'replaceNewlines',
  standalone: true,
  pure: true
})
export class ReplaceNewlinesPipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    if (!value) {
      return '';
    }

    // Respeta los parrafos administrados sin guardar HTML en la base de datos.
    return value
      .split('\n')
      .map((paragraph) => `<p>${paragraph.trim()}</p>`)
      .join('');
  }
}

@Component({
  selector: 'app-about-us',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, ReplaceNewlinesPipe],
  templateUrl: './about-us.html',
  styleUrl: './about-us.css'
})

export class AboutUs implements OnInit, OnDestroy {
  activeFilter: 'todos' | 'hotel' | 'lugares' = 'todos';
  currency: Currency = 'USD';
  currencySymbol = '$';
  private subs = new Subscription();
  aboutUsContent: AboutUsPageContent = createEmptyAboutUsPageContent();

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

  constructor(
    public currencyService: CurrencyService,
    private aboutUsContentService: AboutUsContentService,
    private changeDetectorRef: ChangeDetectorRef,
    private ngZone: NgZone
  ) {}

  ngOnInit(): void {
    this.subs.add(
      this.currencyService.currencyChanges$.subscribe(curr => {
        this.currency = curr;
        this.currencySymbol = this.currencyService.symbol(curr);
      })
    );

    // Carga inicial del contenido dinamico publicado desde el panel admin.
    void this.loadAboutUsContent();

    this.subs.add(
      // Refresca periodicamente para reflejar cambios recientes sin recompilar el cliente.
      timer(10000, 10000)
        .subscribe(() => void this.loadAboutUsContent())
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

  getInitials(name: string): string {
    return name
      .split(' ')
      .slice(0, 2)
      .map((word) => word.charAt(0))
      .join('')
      .toUpperCase();
  }

  private async loadAboutUsContent(): Promise<void> {
    try {
      this.applyAboutUsContent(await firstValueFrom(this.aboutUsContentService.getContent()));
    } catch (error) {
      console.error('Error loading about us content:', error);
      await this.loadAboutUsContentWithFetch();
    }
  }

  private async loadAboutUsContentWithFetch(): Promise<void> {
    // Respaldo directo al proxy local si HttpClient falla durante desarrollo.
    try {
      const response = await fetch('/api/about-us-content', { cache: 'no-store' });
      if (!response.ok) {
        throw new Error(`About Us request failed with status ${response.status}`);
      }

      const content = await response.json();
      this.ngZone.run(() => {
        this.applyAboutUsContent(content);
      });
    } catch (error) {
      console.error('Error loading about us content with fetch:', error);
    }
  }

  private applyAboutUsContent(content: Partial<AboutUsPageContent> | null | undefined): void {
    // Aplica el contenido normalizado y fuerza refresco visual tras respuestas asincronas.
    this.aboutUsContent = normalizeAboutUsPageContent(content);
    this.changeDetectorRef.detectChanges();
  }
}
