import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Narvar } from './modules/narvar/narvar';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, Narvar],
  templateUrl: './app.html',
  styleUrls: ['./app.css']
})
export class App {}
