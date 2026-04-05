import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { HttpClient, HttpClientModule, HttpErrorResponse } from '@angular/common/http';

type RoomId = 'doble' | 'suite' | 'villa';

interface Room {
  id: RoomId;
  nombre: string;
  descripcion: string;
  capacidad: string;
  precioBaja: number; // precio por noche en temporada baja
  multiplicadorAlta: number; // multiplicador para temporada alta
}

@Component({
  selector: 'app-quote',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, HttpClientModule],
  templateUrl: './quote.html',
  styleUrl: './quote.css'
})
export class QuoteComponent implements OnInit {
  habitaciones: Room[] = [
    {
      id: 'doble',
      nombre: 'Habitación Doble',
      descripcion: 'Cama queen, balcón al bosque y café de cortesía.',
      capacidad: '2 personas',
      precioBaja: 95,
      multiplicadorAlta: 1.25
    },
    {
      id: 'suite',
      nombre: 'Suite Volcán',
      descripcion: 'Jacuzzi, terraza panorámica y cóctel de bienvenida.',
      capacidad: 'Hasta 4 personas',
      precioBaja: 135,
      multiplicadorAlta: 1.25
    },
    {
      id: 'villa',
      nombre: 'Villa Familiar',
      descripcion: 'Hasta 5 huéspedes, cocina equipada y chimenea.',
      capacidad: 'Hasta 7 personas',
      precioBaja: 180,
      multiplicadorAlta: 1.25
    }
  ];

  habitacionSeleccionada: Room = this.habitaciones[0];
  fechaInicio = '';
  fechaFin = '';

  nochesTotales = 0;
  nochesAlta = 0;
  nochesBaja = 0;
  total = 0;
  mensajeError = '';

  private readonly apiUrl = '/api/quote/calculate';

  constructor(private route: ActivatedRoute, private http: HttpClient) {}

  ngOnInit(): void {
    const roomParam = this.route.snapshot.queryParamMap.get('habitacion') as RoomId | null;
    if (roomParam) {
      const encontrada = this.habitaciones.find(h => h.id === roomParam);
      if (encontrada) this.habitacionSeleccionada = encontrada;
    }
  }

  onRoomChange(id: string): void {
    const encontrada = this.habitaciones.find(h => h.id === id);
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
    if (!this.fechaInicio) return;
    const inicio = new Date(this.fechaInicio);
    if (!this.fechaFin) {
      const siguiente = new Date(inicio);
      siguiente.setDate(inicio.getDate() + 1);
      this.fechaFin = siguiente.toISOString().substring(0, 10);
      return;
    }

    const fin = new Date(this.fechaFin);
    if (fin <= inicio) {
      const siguiente = new Date(inicio);
      siguiente.setDate(inicio.getDate() + 1);
      this.fechaFin = siguiente.toISOString().substring(0, 10);
    }
  }

  private calcular(): void {
    this.mensajeError = '';
    this.total = 0;
    this.nochesTotales = 0;
    this.nochesAlta = 0;
    this.nochesBaja = 0;

    if (!this.fechaInicio || !this.fechaFin) return;

    const inicio = new Date(this.fechaInicio);
    const fin = new Date(this.fechaFin);
    if (fin <= inicio) {
      this.mensajeError = 'La fecha de salida debe ser posterior a la de entrada.';
      return;
    }

    const body = {
      roomTypeKey: this.habitacionSeleccionada.id,
      startDate: this.fechaInicio,
      endDate: this.fechaFin
    };

    this.http.post<any>(this.apiUrl, body).subscribe({
      next: (res) => {
        this.nochesTotales = res.nightsTotal ?? 0;
        this.nochesAlta = res.nightsHigh ?? 0;
        this.nochesBaja = res.nightsLow ?? 0;
        this.total = res.total ?? 0;
      },
      error: (err: HttpErrorResponse) => {
        this.mensajeError = err.error?.message ?? 'No pudimos calcular la cotización. Intenta de nuevo.';
      }
    });
  }
}
