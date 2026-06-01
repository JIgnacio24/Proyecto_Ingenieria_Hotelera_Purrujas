import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavbarComponent } from './modules/navbar/navbar';
import { FooterComponent } from './modules/footer/footer';
import Contacto from './modules/contacto/contacto';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, NavbarComponent, FooterComponent, Contacto],
  templateUrl: './app.html',
  styleUrls: ['./app.css']
})
export class App {}
