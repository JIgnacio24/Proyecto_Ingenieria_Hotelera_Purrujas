import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export type Currency = 'USD' | 'CRC';

@Injectable({ providedIn: 'root' })
export class CurrencyService {
  private readonly usdToCrcRate = 500;
  private readonly currencySubject = new BehaviorSubject<Currency>('USD');

  currencyChanges$ = this.currencySubject.asObservable();

  get current(): Currency {
    return this.currencySubject.value;
  }

  isValidCurrency(currency: string | null | undefined): currency is Currency {
    return currency === 'USD' || currency === 'CRC';
  }

  setCurrency(currency: Currency): void {
    if (this.isValidCurrency(currency) && currency !== this.current) {
      this.currencySubject.next(currency);
    }
  }

  convertFromUsd(amountUsd: number, currency: Currency = this.current): number {
    return currency === 'CRC' ? amountUsd * this.usdToCrcRate : amountUsd;
  }

  symbol(currency: Currency = this.current): string {
    return currency === 'CRC' ? '₡' : '$';
  }
}
