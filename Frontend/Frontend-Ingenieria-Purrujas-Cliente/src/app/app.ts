import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Narvar } from './modules/narvar/narvar';
import { FooterComponent } from './modules/footer/footer';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, Narvar, FooterComponent],
  templateUrl: './app.html',
  styleUrls: ['./app.css']
})
export class App {}
