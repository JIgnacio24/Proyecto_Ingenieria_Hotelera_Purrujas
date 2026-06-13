import { Component, inject } from '@angular/core';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { EMPTY } from 'rxjs';
import { catchError, filter } from 'rxjs/operators';
import { AdminAuditLogsService } from './core/admin-audit-logs.service';
import { AuthService } from './core/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly adminAuditLogsService = inject(AdminAuditLogsService);

  private readonly viewKeyByPath = new Map<string, string>([
    ['/panel', 'dashboard'],
    ['/panel/home', 'home-editor'],
    ['/panel/sobre-nosotros', 'about-us-editor'],
    ['/panel/reservas', 'reservations'],
    ['/panel/tipos-habitacion', 'room-types'],
    ['/panel/habitaciones', 'rooms'],
    ['/panel/temporadas', 'seasons'],
    ['/panel/ofertas', 'promotions'],
    ['/panel/bitacoras', 'audit-logs'],
    ['/panel/metricas', 'analytics'],
    ['/panel/publicidad', 'advertising']
  ]);

  private lastRecordedViewKey = '';

  constructor() {
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => this.recordViewEntry(event.urlAfterRedirects));
  }

  private recordViewEntry(url: string): void {
    if (!this.authService.hasValidSession()) {
      return;
    }

    const path = url.split('?')[0].split('#')[0];
    const viewKey = this.viewKeyByPath.get(path);
    if (!viewKey || viewKey === this.lastRecordedViewKey) {
      return;
    }

    this.lastRecordedViewKey = viewKey;
    this.adminAuditLogsService
      .recordViewEntry(viewKey)
      .pipe(catchError(() => EMPTY))
      .subscribe();
  }
}
