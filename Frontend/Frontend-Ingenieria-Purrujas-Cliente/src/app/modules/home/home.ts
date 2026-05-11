import { ChangeDetectorRef, Component, NgZone, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { FacilitiesComponent } from '../facilities/facilities.component';
import { GettingThereComponent } from '../getting-there/getting-there.component';
import { OnlineReservationCtaComponent } from '../online-reservation-cta/online-reservation-cta.component';
import { Currency, CurrencyService } from '../../shared/currency.service';
import { GalleryImagesService } from '../../services/galleryImages.service';
import { firstValueFrom, Subscription, timer } from 'rxjs';
import {
  createDefaultHomePageContent,
  HomeContentService,
  HomePageContent
} from '../../services/home-content.service';

const API_ASSET_BASE_URL = 'http://localhost:5232';
const DEFAULT_HERO_IMAGE = '/images/foto_fondo.png';
const HERO_OVERLAY = 'linear-gradient(160deg, rgba(26,58,42,0.82) 0%, rgba(45,106,79,0.65) 60%, rgba(82,183,136,0.3) 100%)';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, FacilitiesComponent, GettingThereComponent, OnlineReservationCtaComponent],
  templateUrl: './home.html',
  styleUrl: './home.css'
})
export class Home implements OnInit, OnDestroy {
  currency: Currency = 'USD';
  currencySymbol = '$';
  heroBackgroundImage = this.toHeroBackgroundImage(DEFAULT_HERO_IMAGE);
  homeContent: HomePageContent = createDefaultHomePageContent();
  private subs = new Subscription();

  constructor(
    public currencyService: CurrencyService,
    private galleryImagesService: GalleryImagesService,
    private homeContentService: HomeContentService,
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

    this.subs.add(
      // Refresca el hero para reflejar cambios publicados desde el panel admin.
      timer(0, 10000).subscribe(() => {
        void this.loadHomeContent();
        void this.loadHeroImage();
      })
    );
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
  }

  price(basePrice: number): number {
    return this.currency === 'CRC' ? basePrice * 540 : basePrice;
  }

  private async loadHeroImage(): Promise<void> {
    try {
      const images = await firstValueFrom(this.galleryImagesService.getAll());

      const heroImage = images.find((image) => image.category === 'fondo')
        ?? images.find((image) => image.name === 'foto_fondo.png');

      this.setHeroImage(heroImage?.src ?? DEFAULT_HERO_IMAGE);
    } catch (error) {
      console.error('Error loading hero image:', error);
      this.setHeroImage(DEFAULT_HERO_IMAGE);
    }
  }

  private async loadHomeContent(): Promise<void> {
    try {
      // El texto principal del hero se administra desde el panel y se lee desde la API publica.
      this.homeContent = await firstValueFrom(this.homeContentService.getContent());
    } catch (error) {
      console.error('Error loading home content:', error);
      this.homeContent = createDefaultHomePageContent();
    }
  }

  private setHeroImage(src: string): void {
    this.ngZone.run(() => {
      this.heroBackgroundImage = this.toHeroBackgroundImage(src);
      this.changeDetectorRef.detectChanges();
    });
  }

  private toHeroBackgroundImage(src: string): string {
    const assetUrl = this.resolveAssetUrl(src);
    return `${HERO_OVERLAY}, url("${assetUrl}")`;
  }

  private resolveAssetUrl(src: string): string {
    if (src.startsWith('http') || src.startsWith('data:') || src.startsWith('blob:')) {
      return src;
    }

    if (src.startsWith('/uploads/')) {
      return `${API_ASSET_BASE_URL}${src}`;
    }

    return src;
  }
}
