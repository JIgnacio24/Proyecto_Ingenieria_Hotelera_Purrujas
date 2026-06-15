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
import { RoomTypesService, PublicRoomType } from '../../services/room-types.service';

type AvailabilityStatus = 'idle' | 'checking' | 'available' | 'unavailable';
type SubmitState = 'idle' | 'loading' | 'success' | 'error';

interface RoomOption {
  roomTypeId: number;
  id: string;
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

  habitaciones: RoomOption[] = [];
  habitacionesLoading = true;
  habitacionesError = '';

  selectedRoom: RoomOption = EMPTY_ROOM;
  currency: Currency = 'USD';
  currencySymbol = '$';
  fechaInicio = '';
  fechaFin = '';

  /** Porcentaje de descuento detectado (0 = sin promo) */
  promoDescuento = 0;
  /** Nombre de la promo aplicada */
  promoNombre = '';
  /** ID de la promoción activa (null = sin promo) */
  promoId: number | null = null;

  nochesTotales = 0;
  nochesAlta = 0;
  nochesBaja = 0;
  total = 0;
  finalTotal = 0;
  discountAmount = 0;
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
    private route: ActivatedRoute,
    public currencyService: CurrencyService,
    private reservationService: ReservationService,
    private publicidadService: PublicidadService,
    private roomTypesService: RoomTypesService,
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
    return this.finalTotal;
  }

  /** Ahorro absoluto en la moneda seleccionada */
  get promoAhorro(): number {
    return this.discountAmount;
  }

  confirmedReservationSymbol(): string {
    return this.currencyService.symbol(this.confirmedReservationCurrency());
  }

  confirmedReservationTotal(): number {
    if (!this.confirmedReservation) return 0;
    return this.confirmedReservationCurrency() === 'CRC'
      ? this.confirmedReservation.totalCrc
      : this.confirmedReservation.totalUsd;
  }

  price(amountUsd: number): number {
    return this.currencyService.convertFromUsd(amountUsd, this.currency);
  }

  ngOnInit(): void {
    this.subs.add(
      this.currencyService.currencyChanges$.subscribe((curr) => {
        this.currency = curr;
        this.currencySymbol = this.currencyService.symbol(curr);
        if (this.fechaInicio && this.fechaFin && this.nochesTotales > 0) {
          this.calculateQuote();
        } else {
          this.updateDisplayTotal();
        }
      })
    );

    // Pre-cargar parámetros de URL (fechas y habitación)
    const params = this.route.snapshot.queryParamMap;
    if (params.has('inicio')) this.fechaInicio = params.get('inicio')!;
    if (params.has('fin'))    this.fechaFin    = params.get('fin')!;

    // Cargar tipos de habitación desde la API
    this.subs.add(
      this.roomTypesService.getAll().subscribe({
        next: (tipos) => {
          this.habitaciones = tipos.map(mapRoomType);
          this.habitacionesLoading = false;

          // Pre-seleccionar habitación por query param ?habitacion=<roomTypeId>
          const habitacionParam = params.get('habitacion');
          const porId = habitacionParam
            ? this.habitaciones.find(h => h.roomTypeId === +habitacionParam)
            : null;
          this.selectedRoom = porId ?? this.habitaciones[0] ?? EMPTY_ROOM;

          if (this.selectedRoom.roomTypeId > 0 && this.fechaInicio && this.fechaFin) {
            this.refreshQuoteAndAvailability();
          }
          this.cdr.detectChanges();
        },
        error: () => {
          this.habitacionesLoading = false;
          this.habitacionesError = 'No fue posible cargar los tipos de habitación. Por favor recargue la página.';
          this.cdr.detectChanges();
        }
      })
    );
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
  }

  selectRoom(room: RoomOption): void {
    if (!room.roomTypeId) return;
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
    if (!this.canSubmit || !this.selectedRoom.roomTypeId) return;

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
      creditCard: values.creditCard.trim(),
      promotionId: this.promoId ?? undefined
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
    if (!this.fechaInicio || !this.fechaFin || !this.selectedRoom.roomTypeId) return;

    const inicio = new Date(this.fechaInicio + 'T00:00:00');
    const fin = new Date(this.fechaFin + 'T00:00:00');
    if (fin <= inicio) return;

    this.calculateQuote();
    this.checkAvailability();
  }

  private calculateQuote(): void {
    if (!this.selectedRoom.roomTypeId) return;
    this.quoteError = '';
    const payload = {
      roomTypeKey: this.selectedRoom.id,
      startDate: this.fechaInicio,
      endDate: this.fechaFin,
      currency: this.currency
    };

    this.http
      .post<{
        nightsTotal: number;
        nightsHigh: number;
        nightsLow: number;
        basePricePerNight: number;
        subtotal: number;
        discountAmount: number;
        total: number;
        currency: Currency;
        promotionDiscount: number;
        promotionName?: string;
      }>(
        '/api/quote/calculate',
        payload
      )
      .subscribe({
        next: (r) => {
          if (r.currency !== this.currency) return;

          this.nochesTotales = r.nightsTotal;
          this.nochesAlta = r.nightsHigh;
          this.nochesBaja = r.nightsLow;
          this.total = r.subtotal;
          this.finalTotal = r.total;
          this.discountAmount = r.discountAmount;
          this.promoDescuento = r.promotionDiscount;
          this.promoNombre = r.promotionName || '';
          if (r.currency === 'CRC') {
            this.totalCrc = r.total;
            this.totalUsd = r.total / 500;
          } else {
            this.totalUsd = r.total;
            this.totalCrc = r.total * 500;
          }
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.resetQuoteAndAvailability();
          this.quoteError = err?.error?.message || 'No se pudo calcular el precio.';
        }
      });
  }

  private checkAvailability(): void {
    if (!this.selectedRoom.roomTypeId) return;
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

    const roomTypeId = this.selectedRoom.roomTypeId;
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
      this.promoId = null;
    } else {
      const mejor = candidatas.reduce((best, p) => p.discount > best.discount ? p : best);
      this.promoDescuento = mejor.discount;
      this.promoNombre = mejor.name;
      this.promoId = mejor.promotionId;
    }

    this.cdr.detectChanges();
  }

  private updateDisplayTotal(): void {
    this.finalTotal = this.currency === 'CRC' ? this.totalCrc : this.totalUsd;
  }

  private confirmedReservationCurrency(): Currency {
    const currency = this.confirmedReservation?.currency;
    return this.currencyService.isValidCurrency(currency) ? currency : 'USD';
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
    this.finalTotal = 0;
    this.discountAmount = 0;
    this.totalUsd = 0;
    this.totalCrc = 0;
    this.quoteError = '';
    this.availabilityStatus = 'idle';
    this.availableRooms = 0;
    this.availabilityError = '';
    this.promoDescuento = 0;
    this.promoNombre = '';
    this.promoId = null;
  }

  private formatDate(d: Date): string {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }
}
