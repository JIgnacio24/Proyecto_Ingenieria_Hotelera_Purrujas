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
import { GalleryImage, GalleryImagesService } from '../../services/galleryImages.service';
import { GettingThereComponent } from '../getting-there/getting-there.component';
import { OnlineReservationCtaComponent } from '../online-reservation-cta/online-reservation-cta.component';

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
  imports: [CommonModule, FormsModule, RouterModule, ReplaceNewlinesPipe, GettingThereComponent, OnlineReservationCtaComponent],
  templateUrl: './about-us.html',
  styleUrl: './about-us.css'
})

export class AboutUs implements OnInit, OnDestroy {
  activeFilter: 'todos' | 'hotel' | 'lugares' = 'todos';
  currency: Currency = 'USD';
  currencySymbol = '$';
  private subs = new Subscription();
  aboutUsContent: AboutUsPageContent = createEmptyAboutUsPageContent();

  galleryItems: GalleryImage[] = [];

  get filteredItems(): GalleryImage[] {
    if (this.activeFilter === 'todos') {
      return this.galleryItems.filter((item) => item.id !== 3);
    }

    return this.galleryItems.filter(item => item.id !== 3 && item.category === this.activeFilter);
  }

  constructor(
    public currencyService: CurrencyService,
    private aboutUsContentService: AboutUsContentService,
    private galleryImagesService: GalleryImagesService,
    private changeDetectorRef: ChangeDetectorRef,
    private ngZone: NgZone
  ) { }

  ngOnInit(): void {
    this.subs.add(
      this.currencyService.currencyChanges$.subscribe(curr => {
        this.currency = curr;
        this.currencySymbol = this.currencyService.symbol(curr);
      })
    );

    // Carga inicial del contenido dinamico publicado desde el panel admin.
    void this.loadAboutUsContent();
    void this.loadGalleryImages();

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

  trackBySrc(_index: number, item: GalleryImage): string {
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

  private async loadGalleryImages(): Promise<void> {
  try {
    const images = await firstValueFrom(this.galleryImagesService.getAll());

    this.galleryItems = images.map((image) => ({
      ...image,
      src: `http://localhost:5232${image.src}`
    }));

    this.changeDetectorRef.detectChanges();
  } catch (error) {
    console.error('Error loading gallery images:', error);
  }
}
}
