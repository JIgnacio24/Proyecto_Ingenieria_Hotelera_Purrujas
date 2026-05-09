import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { FacilitiesComponent } from '../facilities/facilities.component';
import { GettingThereComponent } from '../getting-there/getting-there.component';
import { Currency, CurrencyService } from '../../shared/currency.service';
import { GalleryImagesService } from '../../services/galleryImages.service';
import { firstValueFrom, Subscription, timer } from 'rxjs';
import {
  createDefaultHomePageContent,
  HomeContentService,
  HomePageContent
} from '../../services/home-content.service';



@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, FacilitiesComponent, GettingThereComponent],
  templateUrl: './home.html',
  styleUrl: './home.css'
})
export class Home implements OnInit, OnDestroy {
  currency: Currency = 'USD';
  currencySymbol = '$';
  heroImageUrl = "heroImageUrl = this.toCssImageUrl(${this.apiBaseUrl}/uploads/gallery/foto_fondo.png);";
  homeContent: HomePageContent = createDefaultHomePageContent();
  private subs = new Subscription();

constructor(
  public currencyService: CurrencyService,
  private galleryImagesService: GalleryImagesService,
  private homeContentService: HomeContentService
) {}

  ngOnInit(): void {
    
    this.subs.add(
      
      this.currencyService.currencyChanges$.subscribe(curr => {
        this.currency = curr;
        this.currencySymbol = this.currencyService.symbol(curr);
      })
    );
    void this.loadHomeContent();
    void this.loadHeroImage();

    this.subs.add(
      // Refresca el hero para reflejar cambios publicados desde el panel admin.
      timer(10000, 10000).subscribe(() => {
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

      if (heroImage) {
        this.heroImageUrl = `url('http://localhost:5232${heroImage.src}')`;
      }
    } catch (error) {
      console.error('Error loading hero image:', error);
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
}
