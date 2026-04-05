import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AboutUs } from './modules/about-us/about-us';
import { Promociones } from './modules/promociones/promociones';

export const routes: Routes = [
  { path: '', component: AboutUs },
  { path: 'about-us', component: AboutUs },
  { path: 'promociones', component: Promociones }
];
