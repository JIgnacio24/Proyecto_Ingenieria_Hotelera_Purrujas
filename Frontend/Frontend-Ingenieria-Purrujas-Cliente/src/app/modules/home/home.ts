import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { FacilitiesComponent } from '../facilities/facilities.component';
import { GettingThereComponent } from '../getting-there/getting-there.component';
import { Currency, CurrencyService } from '../../shared/currency.service';
import { Subscription } from 'rxjs';

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
  private subs = new Subscription();

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

  price(basePrice: number): number {
    return this.currency === 'CRC' ? basePrice * 540 : basePrice;
  }
}
