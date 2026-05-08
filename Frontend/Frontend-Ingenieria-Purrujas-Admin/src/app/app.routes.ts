import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';
import { AuthShellComponent } from './pages/auth-shell/auth-shell.component';
import { AboutUsEditorComponent } from './pages/about-us-editor/about-us-editor.component';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { ReservationsComponent } from './pages/reservations/reservations.component';
import { HomeEditorComponent } from './pages/home-editor/home-editor.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'ingreso' },
  { path: 'ingreso', component: AuthShellComponent },
  { path: 'panel', component: DashboardComponent, canActivate: [authGuard] },
  { path: 'panel/home', component: HomeEditorComponent, canActivate: [authGuard] },
  { path: 'panel/sobre-nosotros', component: AboutUsEditorComponent, canActivate: [authGuard] },
  { path: 'panel/reservas', component: ReservationsComponent, canActivate: [authGuard] },
  { path: '**', redirectTo: 'ingreso' }
];
