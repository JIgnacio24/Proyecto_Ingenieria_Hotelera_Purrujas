import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';
import { AuthShellComponent } from './pages/auth-shell/auth-shell.component';
import { AboutUsEditorComponent } from './pages/about-us-editor/about-us-editor.component';
import { AnalyticsComponent } from './pages/analytics/analytics.component';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { ReservationsComponent } from './pages/reservations/reservations.component';
import { HomeEditorComponent } from './pages/home-editor/home-editor.component';
import { RoomsComponent } from './pages/rooms/rooms.component';
import { RoomTypesComponent } from './pages/room-types/room-types.component';
import { SeasonsComponent } from './pages/seasons/seasons.component';
import { PromotionsComponent } from './pages/promotions/promotions.component';
import { AuditLogsComponent } from './pages/audit-logs/audit-logs.component';
import { AdvertisingComponent } from './pages/advertising/advertising.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'ingreso' },
  { path: 'ingreso', component: AuthShellComponent },
  { path: 'panel', component: DashboardComponent, canActivate: [authGuard] },
  { path: 'panel/home', component: HomeEditorComponent, canActivate: [authGuard] },
  { path: 'panel/sobre-nosotros', component: AboutUsEditorComponent, canActivate: [authGuard] },
  { path: 'panel/reservas', component: ReservationsComponent, canActivate: [authGuard] },
  { path: 'panel/tipos-habitacion', component: RoomTypesComponent, canActivate: [authGuard] },
  { path: 'panel/habitaciones', component: RoomsComponent, canActivate: [authGuard] },
  { path: 'panel/temporadas', component: SeasonsComponent, canActivate: [authGuard] },
  { path: 'panel/ofertas', component: PromotionsComponent, canActivate: [authGuard] },
  { path: 'panel/bitacoras', component: AuditLogsComponent, canActivate: [authGuard] },
  { path: 'panel/metricas', component: AnalyticsComponent, canActivate: [authGuard] },
  { path: 'panel/publicidad', component: AdvertisingComponent, canActivate: [authGuard] },
  {
    path: 'panel/predicciones',
    loadComponent: () =>
      import('./pages/occupancy-forecast/occupancy-forecast.component').then(
        (m) => m.OccupancyForecastComponent
      ),
    canActivate: [authGuard]
  },
  { path: '**', redirectTo: 'ingreso' }
];
