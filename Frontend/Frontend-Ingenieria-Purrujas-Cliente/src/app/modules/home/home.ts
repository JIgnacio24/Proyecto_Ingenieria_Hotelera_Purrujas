import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { FacilitiesComponent } from '../facilities/facilities.component';
import { Currency, CurrencyService } from '../../shared/currency.service';
import { GalleryImagesService } from '../../services/galleryImages.service';
import { firstValueFrom, Subscription } from 'rxjs';



@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, FacilitiesComponent],
  templateUrl: './home.html',
  styleUrl: './home.css'
})
export class Home implements OnInit, OnDestroy {
  currency: Currency = 'USD';
  currencySymbol = '$';
  heroImageUrl = "url('/images/foto_fondo.png')";
  private subs = new Subscription();

constructor(
  public currencyService: CurrencyService,
  private galleryImagesService: GalleryImagesService
) {}

  ngOnInit(): void {
    
    this.subs.add(
      
      this.currencyService.currencyChanges$.subscribe(curr => {
        this.currency = curr;
        this.currencySymbol = this.currencyService.symbol(curr);
      })
    );
    void this.loadHeroImage();
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
}
