import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { Home } from './modules/home/home';
import { AboutUs } from './modules/about-us/about-us';
import { FacilitiesComponent } from './modules/facilities/facilities.component';
import { QuoteComponent } from './modules/quote/quote';
import { Promociones } from './modules/promociones/promociones';
import { PublicidadComponent } from './modules/publicidad/publicidad';
import { ReservarComponent } from './modules/reservar/reservar';

export const routes: Routes = [
  { path: '', component: Home },
  { path: 'about-us', component: AboutUs },
  { path: 'facilities', component: FacilitiesComponent },
  { path: 'cotizar', component: QuoteComponent },
  { path: 'promociones', component: Promociones },
  { path: 'publicidad', component: PublicidadComponent },
  { path: 'reservar', component: ReservarComponent },
  { path: '**', redirectTo: '' }
];
