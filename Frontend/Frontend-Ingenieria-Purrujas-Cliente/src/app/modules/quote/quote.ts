import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';

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
  imports: [CommonModule, FormsModule, RouterModule],
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

  constructor(private route: ActivatedRoute) {}

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

  private esTemporadaAlta(fecha: Date): boolean {
    const mes = fecha.getMonth(); // 0-11
    const dia = fecha.getDate();

    // Meses completos de alta: enero, julio, agosto, diciembre
    const mesesAlta = [0, 6, 7, 11];
    if (mesesAlta.includes(mes)) return true;

    // Ventana aproximada de Semana Santa (2026): 29 marzo - 5 abril
    const esSemanaSanta =
      (mes === 2 && dia >= 29) || (mes === 3 && dia <= 5);

    return esSemanaSanta;
  }

  private calcularNoches(): { noches: number; alta: number; baja: number } {
    if (!this.fechaInicio || !this.fechaFin) return { noches: 0, alta: 0, baja: 0 };

    const inicio = new Date(this.fechaInicio);
    const fin = new Date(this.fechaFin);

    if (isNaN(inicio.getTime()) || isNaN(fin.getTime())) return { noches: 0, alta: 0, baja: 0 };
    if (fin <= inicio) return { noches: 0, alta: 0, baja: 0 };

    let cursor = new Date(inicio);
    let noches = 0;
    let alta = 0;
    let baja = 0;

    while (cursor < fin) {
      noches++;
      if (this.esTemporadaAlta(cursor)) alta++; else baja++;
      cursor.setDate(cursor.getDate() + 1);
    }

    return { noches, alta, baja };
  }

  private calcular(): void {
    this.mensajeError = '';
    const { noches, alta, baja } = this.calcularNoches();
    this.nochesTotales = noches;
    this.nochesAlta = alta;
    this.nochesBaja = baja;

    if (!this.fechaInicio || !this.fechaFin) {
      this.total = 0;
      return;
    }

    const inicio = new Date(this.fechaInicio);
    const fin = new Date(this.fechaFin);
    if (fin <= inicio) {
      this.mensajeError = 'La fecha de salida debe ser posterior a la de entrada.';
      this.total = 0;
      return;
    }

    const precioAlta = this.habitacionSeleccionada.precioBaja * this.habitacionSeleccionada.multiplicadorAlta;
    this.total = (baja * this.habitacionSeleccionada.precioBaja) + (alta * precioAlta);
  }
}
