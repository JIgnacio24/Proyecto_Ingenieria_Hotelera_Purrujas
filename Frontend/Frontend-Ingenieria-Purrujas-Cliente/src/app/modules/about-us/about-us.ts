import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FacilitiesComponent } from '../facilities/facilities.component';
 
@Component({
  selector: 'app-about-us',
  standalone: true,
  imports: [CommonModule, RouterModule, FacilitiesComponent],
  templateUrl: './about-us.html',
  styleUrl: './about-us.css'
})

export class AboutUs {}
