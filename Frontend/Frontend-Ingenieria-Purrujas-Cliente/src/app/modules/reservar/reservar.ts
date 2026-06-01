import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { forkJoin, of, Subscription } from 'rxjs';
import { delay } from 'rxjs/operators';
import { Currency, CurrencyService } from '../../shared/currency.service';
import { ReservationService, ReservationResponse } from '../../services/reservation.service';
import { PublicidadService, Promocion } from '../../services/publicidad.service';

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

/** Mapea el id de habitación al roomTypeId del backend */
const ROOM_TYPE_ID: Record<RoomId, number> = {
  doble: 1,
  suite: 2,
  villa: 3
};

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

  /** Porcentaje de descuento detectado (0 = sin promo) */
  promoDescuento = 0;
  /** Nombre de la promo aplicada */
  promoNombre = '';

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
  private promociones: Promocion[] = [];

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private route: ActivatedRoute,
    public currencyService: CurrencyService,
    private reservationService: ReservationService,
    private publicidadService: PublicidadService,
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

  /** Precio final después de aplicar el descuento promocional */
  get totalConDescuento(): number {
    if (this.promoDescuento <= 0 || this.total <= 0) return this.total;
    return Math.round(this.total * (1 - this.promoDescuento / 100) * 100) / 100;
  }

  /** Ahorro absoluto en la moneda seleccionada */
  get promoAhorro(): number {
    return Math.round((this.total - this.totalConDescuento) * 100) / 100;
  }

  ngOnInit(): void {
    this.subs.add(
      this.currencyService.currencyChanges$.subscribe((curr) => {
        this.currency = curr;
        this.currencySymbol = this.currencyService.symbol(curr);
        this.updateDisplayTotal();
      })
    );

    // Cargar catálogo de promociones para detección automática
    this.subs.add(
      this.publicidadService.getPromociones().subscribe(promos => {
        this.promociones = promos;
        // Re-detectar si ya hay fechas (ej: llegó desde el botón de promo)
        if (this.fechaInicio && this.fechaFin) this.detectPromo();
      })
    );

    // Pre-cargar datos cuando se llega desde el botón "Aprovechar oferta"
    const params = this.route.snapshot.queryParamMap;
    if (params.has('inicio')) this.fechaInicio = params.get('inicio')!;
    if (params.has('fin'))    this.fechaFin    = params.get('fin')!;
    if (params.has('habitacion')) {
      const room = this.habitaciones.find(h => h.id === params.get('habitacion'));
      if (room) this.selectedRoom = room;
    }
    if (this.fechaInicio && this.fechaFin) {
      this.refreshQuoteAndAvailability();
    }
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

    forkJoin([
      this.reservationService.createReservation(request),
      of(null).pipe(delay(3000))
    ]).subscribe({
      next: ([response]) => {
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

  // ── Privados ──────────────────────────────────────────────────────────────

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
          // Detectar promo aplicable una vez que tenemos el precio base
          this.detectPromo();
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
    forkJoin([
      this.reservationService.checkAvailability(this.selectedRoom.id, this.fechaInicio, this.fechaFin),
      of(null).pipe(delay(3000))
    ]).subscribe({
      next: ([r]) => {
        this.availabilityError = '';
        this.availabilityStatus = r.isAvailable ? 'available' : 'unavailable';
        this.availableRooms = r.availableRooms;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.availabilityStatus = 'unavailable';
        this.availabilityError =
          err?.error?.message || 'No se pudo verificar disponibilidad. Intente de nuevo.';
        this.cdr.detectChanges();
      }
    });
  }

  /**
   * Busca la mejor promoción (mayor descuento) cuyo período de vigencia
   * se solape con las fechas seleccionadas y corresponda al tipo de habitación activo.
   */
  private detectPromo(): void {
    if (!this.fechaInicio || !this.fechaFin || this.promociones.length === 0) {
      this.promoDescuento = 0;
      this.promoNombre = '';
      return;
    }

    const roomTypeId = ROOM_TYPE_ID[this.selectedRoom.id];
    const inicio = new Date(this.fechaInicio + 'T00:00:00');
    const fin    = new Date(this.fechaFin    + 'T00:00:00');

    const candidatas = this.promociones.filter(p =>
      p.roomTypeId === roomTypeId &&
      new Date(p.startDate + (p.startDate.includes('T') ? '' : 'T00:00:00')) <= fin &&
      new Date(p.endDate   + (p.endDate.includes('T')   ? '' : 'T00:00:00')) >= inicio
    );

    if (candidatas.length === 0) {
      this.promoDescuento = 0;
      this.promoNombre = '';
    } else {
      const mejor = candidatas.reduce((best, p) => p.discount > best.discount ? p : best);
      this.promoDescuento = mejor.discount;
      this.promoNombre = mejor.name;
    }

    this.cdr.detectChanges();
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
    this.promoDescuento = 0;
    this.promoNombre = '';
  }

  private formatDate(d: Date): string {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }
}
