import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Subscription } from 'rxjs';
import { Currency, CurrencyService } from '../../shared/currency.service';
import { ReservationService, ReservationResponse } from '../../services/reservation.service';

type RoomId = 'doble' | 'suite' | 'villa';
type AvailabilityStatus = 'idle' | 'checking' | 'available' | 'unavailable';
type SubmitState = 'idle' | 'loading' | 'success' | 'error';

interface RoomOption {
  id: RoomId;
  nombre: string;
  descripcion: string;
  capacidad: string;
  precioBaja: number;
  icon: string;
}

@Component({
  selector: 'app-reservar',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterModule],
  templateUrl: './reservar.html',
  styleUrl: './reservar.css'
})
export class ReservarComponent implements OnInit, OnDestroy {
  readonly minStartDate = this.formatDate(new Date());

  readonly habitaciones: RoomOption[] = [
    {
      id: 'doble',
      nombre: 'Habitación Doble',
      descripcion: 'Cama queen, balcón al bosque y café de cortesía.',
      capacidad: '2 personas',
      precioBaja: 95,
      icon: '🛏️'
    },
    {
      id: 'suite',
      nombre: 'Suite Volcán',
      descripcion: 'Jacuzzi, terraza panorámica y cóctel de bienvenida.',
      capacidad: 'Hasta 4 personas',
      precioBaja: 135,
      icon: '🌋'
    },
    {
      id: 'villa',
      nombre: 'Villa Familiar',
      descripcion: 'Hasta 5 huéspedes, cocina equipada y chimenea.',
      capacidad: 'Hasta 7 personas',
      precioBaja: 180,
      icon: '🏡'
    }
  ];

  selectedRoom: RoomOption = this.habitaciones[0];
  currency: Currency = 'USD';
  currencySymbol = '$';
  fechaInicio = '';
  fechaFin = '';

  nochesTotales = 0;
  nochesAlta = 0;
  nochesBaja = 0;
  total = 0;
  totalUsd = 0;
  totalCrc = 0;
  quoteError = '';

  availabilityStatus: AvailabilityStatus = 'idle';
  availableRooms = 0;
  availabilityError = '';

  submitState: SubmitState = 'idle';
  submitError = '';
  confirmedReservation: ReservationResponse | null = null;

  guestForm: FormGroup;

  private subs = new Subscription();

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    public currencyService: CurrencyService,
    private reservationService: ReservationService,
    private cdr: ChangeDetectorRef
  ) {
    this.guestForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      lastName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.email]],
      phone: [''],
      creditCard: ['', [Validators.required, Validators.pattern(/^\d{13,19}$/)]]
    });
  }

  get minEndDate(): string {
    return this.fechaInicio || this.minStartDate;
  }

  get canSubmit(): boolean {
    return (
      this.availabilityStatus === 'available' &&
      this.nochesTotales > 0 &&
      this.guestForm.valid &&
      this.submitState !== 'loading'
    );
  }

  ngOnInit(): void {
    this.subs.add(
      this.currencyService.currencyChanges$.subscribe((curr) => {
        this.currency = curr;
        this.currencySymbol = this.currencyService.symbol(curr);
        this.updateDisplayTotal();
      })
    );
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
  }

  selectRoom(room: RoomOption): void {
    this.selectedRoom = room;
    this.resetQuoteAndAvailability();
    this.refreshQuoteAndAvailability();
  }

  onCurrencyChange(currency: string): void {
    this.currencyService.setCurrency(currency as Currency);
  }

  onDateChange(): void {
    this.adjustEndDate();
    this.refreshQuoteAndAvailability();
  }

  submitReservation(): void {
    if (!this.canSubmit) return;

    this.guestForm.markAllAsTouched();
    if (this.guestForm.invalid) return;

    this.submitState = 'loading';
    this.submitError = '';

    const values = this.guestForm.value;
    const request = {
      roomTypeKey: this.selectedRoom.id,
      startDate: this.fechaInicio,
      endDate: this.fechaFin,
      currency: this.currency,
      name: values.name.trim(),
      lastName: values.lastName.trim(),
      email: values.email.trim(),
      phone: values.phone?.trim() || undefined,
      creditCard: values.creditCard.trim()
    };

    this.reservationService.createReservation(request).subscribe({
      next: (response) => {
        this.confirmedReservation = response;
        this.submitState = 'success';
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.submitState = 'error';
        this.submitError =
          err?.error?.message || 'Ocurrió un error al confirmar la reserva. Por favor intente de nuevo.';
        this.cdr.detectChanges();
      }
    });
  }

  resetForm(): void {
    this.submitState = 'idle';
    this.submitError = '';
    this.confirmedReservation = null;
    this.guestForm.reset();
    this.fechaInicio = '';
    this.fechaFin = '';
    this.resetQuoteAndAvailability();
  }

  fieldError(fieldName: string): string {
    const control = this.guestForm.get(fieldName);
    if (!control || !control.touched || !control.errors) return '';
    if (control.errors['required']) return 'Este campo es requerido.';
    if (control.errors['email']) return 'Ingrese un correo válido.';
    if (control.errors['minlength']) return `Mínimo ${control.errors['minlength'].requiredLength} caracteres.`;
    if (control.errors['pattern']) return 'Ingrese solo dígitos (13-19 caracteres).';
    return 'Campo inválido.';
  }

  formatDisplayDate(dateStr: string): string {
    if (!dateStr) return '';
    const d = new Date(dateStr + 'T00:00:00');
    return new Intl.DateTimeFormat('es-CR', { day: 'numeric', month: 'long', year: 'numeric' }).format(d);
  }

  private refreshQuoteAndAvailability(): void {
    if (!this.fechaInicio || !this.fechaFin) return;

    const inicio = new Date(this.fechaInicio + 'T00:00:00');
    const fin = new Date(this.fechaFin + 'T00:00:00');
    if (fin <= inicio) return;

    this.calculateQuote();
    this.checkAvailability();
  }

  private calculateQuote(): void {
    this.quoteError = '';
    const payload = {
      roomTypeKey: this.selectedRoom.id,
      startDate: this.fechaInicio,
      endDate: this.fechaFin,
      currency: 'USD'
    };

    this.http
      .post<{ nightsTotal: number; nightsHigh: number; nightsLow: number; basePricePerNight: number; total: number }>(
        '/api/quote/calculate',
        payload
      )
      .subscribe({
        next: (r) => {
          this.nochesTotales = r.nightsTotal;
          this.nochesAlta = r.nightsHigh;
          this.nochesBaja = r.nightsLow;
          this.totalUsd = r.total;
          this.totalCrc = r.total * 500;
          this.updateDisplayTotal();
        },
        error: (err) => {
          this.resetQuoteAndAvailability();
          this.quoteError = err?.error?.message || 'No se pudo calcular el precio.';
        }
      });
  }

  private checkAvailability(): void {
    this.availabilityStatus = 'checking';
    this.availabilityError = '';
    this.reservationService.checkAvailability(this.selectedRoom.id, this.fechaInicio, this.fechaFin).subscribe({
      next: (r) => {
        this.availabilityError = '';
        this.availabilityStatus = r.isAvailable ? 'available' : 'unavailable';
        this.availableRooms = r.availableRooms;
      },
      error: (err) => {
        this.availabilityStatus = 'unavailable';
        this.availabilityError =
          err?.error?.message || 'No se pudo verificar disponibilidad. Intente de nuevo.';
      }
    });
  }

  private updateDisplayTotal(): void {
    this.total = this.currency === 'CRC' ? this.totalCrc : this.totalUsd;
  }

  private adjustEndDate(): void {
    if (!this.fechaInicio) return;
    const inicio = new Date(this.fechaInicio + 'T00:00:00');
    if (!this.fechaFin) {
      const next = new Date(inicio);
      next.setDate(inicio.getDate() + 1);
      this.fechaFin = this.formatDate(next);
      return;
    }
    const fin = new Date(this.fechaFin + 'T00:00:00');
    if (fin <= inicio) {
      const next = new Date(inicio);
      next.setDate(inicio.getDate() + 1);
      this.fechaFin = this.formatDate(next);
    }
  }

  private resetQuoteAndAvailability(): void {
    this.nochesTotales = 0;
    this.nochesAlta = 0;
    this.nochesBaja = 0;
    this.total = 0;
    this.totalUsd = 0;
    this.totalCrc = 0;
    this.quoteError = '';
    this.availabilityStatus = 'idle';
    this.availableRooms = 0;
    this.availabilityError = '';
  }

  private formatDate(d: Date): string {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }
}
