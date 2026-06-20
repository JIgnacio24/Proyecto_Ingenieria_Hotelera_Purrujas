import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Subscription } from 'rxjs';
import { Currency, CurrencyService } from '../../shared/currency.service';
import { RoomTypesService, PublicRoomType } from '../../services/room-types.service';

function formatCapacity(capacity: number): string {
  if (capacity <= 0) return '';
  if (capacity === 1) return '1 persona';
  if (capacity <= 2) return `${capacity} personas`;
  return `Hasta ${capacity} personas`;
}

interface Room {
  roomTypeId: number;
  id: string;
  nombre: string;
  descripcion: string;
  capacidad: string;
  precioBaja: number;
  multiplicadorAlta: number;
}

const EMPTY_ROOM: Room = {
  roomTypeId: 0, id: '', nombre: '', descripcion: '', capacidad: '', precioBaja: 0, multiplicadorAlta: 1.25
};

@Component({
  selector: 'app-quote',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './quote.html',
  styleUrl: './quote.css'
})
export class QuoteComponent implements OnInit, OnDestroy {
  readonly minStartDate = this.formatDateForInput(new Date());

  habitaciones: Room[] = [];
  habitacionesLoading = true;
  habitacionSeleccionada: Room = EMPTY_ROOM;
  fechaInicio = '';
  fechaFin = '';

  nochesTotales = 0;
  nochesAlta = 0;
  nochesBaja = 0;
  total = 0;
  calculando = false;
  mensajeError = '';
  habitacionesError = '';
  currency: Currency = 'USD';
  currencySymbol = '$';

  private subs = new Subscription();

  constructor(
    private route: ActivatedRoute,
    private http: HttpClient,
    public currencyService: CurrencyService,
    private roomTypesService: RoomTypesService
  ) {}

  get minEndDate(): string {
    return this.fechaInicio || this.minStartDate;
  }

  ngOnInit(): void {
    this.subs.add(
      this.roomTypesService.getAll().subscribe({
        next: (tipos) => {
          this.habitacionesError = '';
          this.habitacionesLoading = false;
          this.habitaciones = tipos.map((rt: PublicRoomType) => ({
            roomTypeId: rt.roomTypeId,
            id: rt.name,
            nombre: rt.name,
            descripcion: rt.description ?? '',
            capacidad: formatCapacity(rt.capacity),
            precioBaja: rt.basePrice,
            multiplicadorAlta: 1.25
          }));

          const roomParam = this.route.snapshot.queryParamMap.get('habitacion');
          const porId = roomParam ? this.habitaciones.find(h => h.roomTypeId === +roomParam) : null;
          const porNombre = roomParam ? this.habitaciones.find(h => h.id === roomParam) : null;
          this.habitacionSeleccionada = porId ?? porNombre ?? this.habitaciones[0] ?? EMPTY_ROOM;
          this.calcular();
        },
        error: () => {
          this.habitacionesLoading = false;
          this.habitacionesError = 'No fue posible cargar los tipos de habitación. Por favor recargue la página.';
        }
      })
    );

    this.subs.add(
      this.currencyService.currencyChanges$.subscribe((curr) => {
        this.currency = curr;
        this.currencySymbol = this.currencyService.symbol(curr);
        this.calcular();
      })
    );
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
  }

  onRoomChange(id: string): void {
    const encontrada = this.habitaciones.find((habitacion) => habitacion.id === id);
    if (encontrada) {
      this.habitacionSeleccionada = encontrada;
      this.calcular();
    }
  }

  onDateChange(): void {
    this.ajustarFechaSalida();
    this.calcular();
  }

  private ajustarFechaSalida(): void {
    if (!this.fechaInicio) {
      return;
    }

    const inicio = this.parseDateInput(this.fechaInicio);
    if (!inicio) {
      return;
    }

    if (!this.fechaFin) {
      const siguiente = new Date(inicio);
      siguiente.setDate(inicio.getDate() + 1);
      this.fechaFin = this.formatDateForInput(siguiente);
      return;
    }

    const fin = this.parseDateInput(this.fechaFin);
    if (!fin || fin <= inicio) {
      const siguiente = new Date(inicio);
      siguiente.setDate(inicio.getDate() + 1);
      this.fechaFin = this.formatDateForInput(siguiente);
    }
  }

  private calcular(): void {
    this.mensajeError = '';

    const validationError = this.validateQuoteInput();
    if (validationError) {
      this.resetQuote();
      this.mensajeError = validationError;
      return;
    }

    if (!this.fechaInicio && !this.fechaFin) {
      this.resetQuote();
      return;
    }

    const payload = {
      roomTypeKey: this.habitacionSeleccionada.id,
      startDate: this.fechaInicio,
      endDate: this.fechaFin,
      currency: this.currency
    };

    this.calculando = true;

    this.http
      .post<{
        roomTypeKey: string;
        nightsTotal: number;
        nightsHigh: number;
        nightsLow: number;
        basePricePerNight: number;
        highSeasonMultiplier: number;
        total: number;
        currency: Currency;
      }>('/api/quote/calculate', payload)
      .subscribe({
        next: (response) => {
          this.nochesTotales = response.nightsTotal;
          this.nochesAlta = response.nightsHigh;
          this.nochesBaja = response.nightsLow;
          this.total = response.total;
          this.calculando = false;
        },
        error: (error) => {
          this.resetQuote();
          this.mensajeError = error?.error?.message ?? 'No se pudo calcular la cotización.';
        }
      });
  }

  private validateQuoteInput(): string {
    if (!this.habitacionSeleccionada?.id) {
      return 'Debe seleccionar un tipo de habitación válido.';
    }

    if (!this.currencyService.isValidCurrency(this.currency)) {
      return 'La moneda seleccionada no es válida.';
    }

    if (!this.fechaInicio && !this.fechaFin) {
      return '';
    }

    if (!this.fechaInicio || !this.fechaFin) {
      return 'Debe completar la fecha de entrada y la fecha de salida.';
    }

    const inicio = this.parseDateInput(this.fechaInicio);
    const fin = this.parseDateInput(this.fechaFin);
    if (!inicio || !fin) {
      return 'Las fechas ingresadas no son válidas.';
    }

    const hoy = this.parseDateInput(this.minStartDate);
    if (!hoy) {
      return 'No fue posible validar la fecha actual.';
    }

    if (inicio < hoy) {
      return 'La fecha de entrada no puede estar en el pasado.';
    }

    if (fin <= inicio) {
      return 'La fecha de salida debe ser posterior a la de entrada.';
    }

    return '';
  }

  private resetQuote(): void {
    this.total = 0;
    this.nochesTotales = 0;
    this.nochesAlta = 0;
    this.nochesBaja = 0;
    this.calculando = false;
  }

  private parseDateInput(value: string): Date | null {
    if (!value) {
      return null;
    }

    const date = new Date(`${value}T00:00:00`);
    return Number.isNaN(date.getTime()) ? null : date;
  }

  private formatDateForInput(value: Date): string {
    const year = value.getFullYear();
    const month = String(value.getMonth() + 1).padStart(2, '0');
    const day = String(value.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
