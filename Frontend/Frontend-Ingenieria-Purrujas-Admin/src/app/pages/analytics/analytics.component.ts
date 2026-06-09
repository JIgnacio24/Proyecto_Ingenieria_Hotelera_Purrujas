import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, input, signal } from '@angular/core';
import { RouterModule } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import {
  DashboardAnalyticsService,
  DashboardMetrics,
  MonthlyDataPoint
} from '../../core/dashboard-analytics.service';

export interface BarData {
  x: number;
  y: number;
  width: number;
  height: number;
  label: string;
  formattedValue: string;
}

export interface DonutSegment {
  strokeDasharray: string;
  rotation: number;
  color: string;
  label: string;
  count: number;
  percentage: number;
}

@Component({
  selector: 'app-analytics',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './analytics.component.html',
  styleUrl: './analytics.component.css'
})
export class AnalyticsComponent {
  readonly embedded = input(false);

  private readonly service = inject(DashboardAnalyticsService);
  private readonly authService = inject(AuthService);

  readonly loading = signal(true);
  readonly error = signal('');
  readonly metrics = signal<DashboardMetrics | null>(null);

  private static readonly BAR_H = 150;
  private static readonly BAR_W = 36;
  private static readonly GAP = 14;
  private static readonly MONTHS_ES = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'];
  private static readonly STATUS_COLORS: Record<string, string> = {
    'Confirmada': '#4ade80',
    'Pendiente':  '#facc15',
    'Cancelada':  '#f87171',
    'Finalizada': '#60a5fa'
  };

  constructor() {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.error.set('');
    try {
      const data = await firstValueFrom(this.service.getMetrics());
      this.metrics.set(data);
    } catch (err) {
      if (err instanceof HttpErrorResponse && (err.status === 401 || err.status === 403)) {
        this.authService.logout();
        return;
      }
      this.error.set(
        err instanceof HttpErrorResponse
          ? (err.error?.message ?? err.message)
          : 'Error al cargar las métricas del dashboard.'
      );
    } finally {
      this.loading.set(false);
    }
  }

  // ── Bar charts ─────────────────────────────────────────────────────────────

  reservationBars(): BarData[] {
    return this.buildBars(
      this.metrics()?.reservationsByMonth ?? [],
      (p) => p.count,
      (v) => v.toString()
    );
  }

  revenueBars(): BarData[] {
    return this.buildBars(
      this.metrics()?.revenueByMonth ?? [],
      (p) => p.amount,
      (v) => this.formatUsdShort(v)
    );
  }

  roomTypeBars(): BarData[] {
    const items = this.metrics()?.topRoomTypes ?? [];
    const max = Math.max(...items.map((t) => t.reservationCount), 1);
    const h = AnalyticsComponent.BAR_H;
    const bw = AnalyticsComponent.BAR_W;
    const gap = AnalyticsComponent.GAP;
    return items.map((item, i) => {
      const barH = Math.max((item.reservationCount / max) * h, item.reservationCount > 0 ? 4 : 0);
      return {
        x: i * (bw + gap),
        y: h - barH,
        width: bw,
        height: barH,
        label: item.roomTypeName,
        formattedValue: item.reservationCount.toString()
      };
    });
  }

  svgWidth(count: number): number {
    const { BAR_W, GAP } = AnalyticsComponent;
    return Math.max(count * (BAR_W + GAP) - GAP, 10);
  }

  svgHeight(): number {
    return AnalyticsComponent.BAR_H;
  }

  // ── Donut charts ───────────────────────────────────────────────────────────

  statusSegments(): DonutSegment[] {
    const statuses = this.metrics()?.reservationsByStatus ?? [];
    return this.buildDonut(
      statuses.map((s) => s.count),
      statuses.map((s) => s.status),
      statuses.map((s) => AnalyticsComponent.STATUS_COLORS[s.status] ?? '#94a3b8')
    );
  }

  roomStatusSegments(): DonutSegment[] {
    const rs = this.metrics()?.roomStatus;
    if (!rs) return [];
    return this.buildDonut(
      [rs.available, rs.occupied, rs.maintenance],
      ['Disponible', 'Ocupada', 'Mantenimiento'],
      ['#4ade80', '#fb923c', '#94a3b8']
    );
  }

  // ── KPI helpers ────────────────────────────────────────────────────────────

  occupancyRate(): number {
    const m = this.metrics();
    if (!m || m.kpi.totalRooms === 0) return 0;
    return Math.round((m.kpi.occupiedRooms / m.kpi.totalRooms) * 100);
  }

  totalReservations(): number {
    return (this.metrics()?.reservationsByStatus ?? []).reduce((sum, s) => sum + s.count, 0);
  }

  // ── Formatters ─────────────────────────────────────────────────────────────

  formatUsd(value: number): string {
    return new Intl.NumberFormat('es-CR', {
      style: 'currency',
      currency: 'USD',
      maximumFractionDigits: 2
    }).format(value);
  }

  formatUsdShort(value: number): string {
    if (value >= 1000) {
      return `$${(value / 1000).toFixed(1)}k`;
    }
    return `$${Math.round(value)}`;
  }

  monthLabel(month: number): string {
    return AnalyticsComponent.MONTHS_ES[month - 1] ?? '';
  }

  // ── Private helpers ────────────────────────────────────────────────────────

  private buildBars(
    points: MonthlyDataPoint[],
    getValue: (p: MonthlyDataPoint) => number,
    format: (v: number) => string
  ): BarData[] {
    const h = AnalyticsComponent.BAR_H;
    const bw = AnalyticsComponent.BAR_W;
    const gap = AnalyticsComponent.GAP;
    const values = points.map(getValue);
    const max = Math.max(...values, 1);

    return points.map((p, i) => {
      const v = getValue(p);
      const barH = Math.max((v / max) * h, v > 0 ? 4 : 0);
      return {
        x: i * (bw + gap),
        y: h - barH,
        width: bw,
        height: barH,
        label: AnalyticsComponent.MONTHS_ES[p.month - 1] ?? '',
        formattedValue: format(v)
      };
    });
  }

  private buildDonut(counts: number[], labels: string[], colors: string[]): DonutSegment[] {
    const total = counts.reduce((a, b) => a + b, 0);
    if (total === 0) return [];
    const r = 40;
    const circ = 2 * Math.PI * r;
    let accumulated = 0;

    return counts.map((count, i) => {
      const frac = count / total;
      const dash = frac * circ;
      const rotation = (accumulated / circ) * 360 - 90;
      accumulated += dash;
      return {
        strokeDasharray: `${dash.toFixed(2)} ${(circ - dash).toFixed(2)}`,
        rotation,
        color: colors[i] ?? '#94a3b8',
        label: labels[i] ?? '',
        count,
        percentage: Math.round(frac * 100)
      };
    });
  }
}
