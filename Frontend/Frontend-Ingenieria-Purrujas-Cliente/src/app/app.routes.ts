import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AboutUs } from './modules/about-us/about-us';
import { QuoteComponent } from './modules/quote/quote';
import { Narvar } from './modules/narvar/narvar';
import { Promociones } from './modules/promociones/promociones';

export const routes: Routes = [
  { path: '', component: AboutUs },
  { path: 'about-us', component: AboutUs },
  { path: 'cotizar', component: QuoteComponent },
  { path: 'navbar', component: Narvar },
  { path: 'promociones', component: Promociones }
];
