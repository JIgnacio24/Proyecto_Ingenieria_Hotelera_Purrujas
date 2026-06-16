import { CommonModule } from '@angular/common';
import {
  AfterViewInit,
  Component,
  ElementRef,
  OnDestroy,
  ViewChild,
  inject,
  signal
} from '@angular/core';
import { RouterModule } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { Chart, ChartConfiguration, registerables } from 'chart.js';
import { firstValueFrom } from 'rxjs';
import { OccupancyPrediction, PredictionService } from '../../core/prediction.service';

Chart.register(...registerables);

const MONTHS_ES = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'];

@Component({
  selector: 'app-occupancy-forecast',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './occupancy-forecast.component.html',
  styleUrl: './occupancy-forecast.component.css'
})
export class OccupancyForecastComponent implements AfterViewInit, OnDestroy {
  @ViewChild('chartCanvas') private chartCanvas!: ElementRef<HTMLCanvasElement>;

  private readonly predictionService = inject(PredictionService);
  private chart: Chart | null = null;

  readonly loading = signal(true);
  readonly error = signal('');

  async ngAfterViewInit(): Promise<void> {
    try {
      const [history, predictions] = await Promise.all([
        firstValueFrom(this.predictionService.getHistory()),
        firstValueFrom(this.predictionService.getPredictions())
      ]);

      const recentHistory = history.slice(-12);
      this.renderChart(recentHistory, predictions);
    } catch (err: unknown) {
      const msg = this.resolveError(err);
      console.error('[OccupancyForecast] Error cargando datos:', err);
      this.error.set(msg);
    } finally {
      this.loading.set(false);
    }
  }

  private resolveError(err: unknown): string {
    if (err instanceof HttpErrorResponse) {
      if (err.status === 401) return 'Sesión expirada. Por favor, inicia sesión nuevamente.';
      if (err.status === 0) return 'No se pudo conectar con el servidor. Verifica que el backend esté activo.';
      const detail = err.error?.message ?? err.error?.detail ?? err.message;
      return `Error ${err.status}: ${detail ?? 'Error desconocido del servidor.'}`;
    }
    if (err instanceof Error) return err.message;
    return 'No se pudo cargar la información de ocupación.';
  }

  ngOnDestroy(): void {
    this.chart?.destroy();
  }

  private formatLabel(item: OccupancyPrediction): string {
    return `${MONTHS_ES[item.month - 1]} ${item.year}`;
  }

  private renderChart(history: OccupancyPrediction[], predictions: OccupancyPrediction[]): void {
    const historyLabels = history.map(d => this.formatLabel(d));
    const predictionLabels = predictions.map(d => this.formatLabel(d));
    const allLabels = [...historyLabels, ...predictionLabels];

    // El dataset de proyección tiene nulls en las posiciones del historial
    // para que la línea comience donde termina el historial
    const historyValues = history.map(d => d.occupancyPercentage);
    const lastHistoryValue = historyValues[historyValues.length - 1] ?? null;
    const predictionValues: (number | null)[] = [
      ...Array(history.length - 1).fill(null),
      lastHistoryValue,
      ...predictions.map(d => d.occupancyPercentage)
    ];

    const config: ChartConfiguration<'line'> = {
      type: 'line',
      data: {
        labels: allLabels,
        datasets: [
          {
            label: 'Historial de Ocupación',
            data: historyValues,
            borderColor: '#3b82f6',
            backgroundColor: 'rgba(59, 130, 246, 0.1)',
            borderWidth: 2,
            pointRadius: 4,
            pointBackgroundColor: '#3b82f6',
            tension: 0.3,
            fill: true
          },
          {
            label: 'Proyección (6 meses)',
            data: predictionValues,
            borderColor: '#f97316',
            backgroundColor: 'rgba(249, 115, 22, 0.08)',
            borderWidth: 2,
            borderDash: [6, 4],
            pointRadius: 4,
            pointBackgroundColor: '#f97316',
            tension: 0.3,
            fill: false,
            spanGaps: false
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        interaction: { mode: 'index', intersect: false },
        plugins: {
          legend: { position: 'top' },
          tooltip: {
            callbacks: {
              label: ctx => `${ctx.dataset.label}: ${(ctx.parsed.y as number).toFixed(1)}%`
            }
          }
        },
        scales: {
          y: {
            min: 0,
            max: 100,
            ticks: { callback: val => `${val}%` },
            title: { display: true, text: 'Ocupación (%)' }
          },
          x: {
            title: { display: true, text: 'Mes' }
          }
        }
      }
    };

    this.chart = new Chart(this.chartCanvas.nativeElement, config);
  }
}
