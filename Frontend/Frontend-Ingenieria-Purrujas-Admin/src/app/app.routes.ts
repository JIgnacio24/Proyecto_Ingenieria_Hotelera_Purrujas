import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';
import { AuthShellComponent } from './pages/auth-shell/auth-shell.component';
import { AboutUsEditorComponent } from './pages/about-us-editor/about-us-editor.component';
import { DashboardComponent } from './pages/dashboard/dashboard.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'ingreso' },
  { path: 'ingreso', component: AuthShellComponent },
  { path: 'panel', component: DashboardComponent, canActivate: [authGuard] },
  { path: 'panel/sobre-nosotros', component: AboutUsEditorComponent, canActivate: [authGuard] },
  { path: '**', redirectTo: 'ingreso' }
];
