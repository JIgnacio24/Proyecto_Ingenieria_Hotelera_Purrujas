import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ChangeDetectorRef } from '@angular/core';

import { Narvar } from '../narvar/narvar';
import { Publicidad, PublicidadService } from '../../services/publicidad.service';

@Component({
  selector: 'app-publicidad',
  standalone: true,
  imports: [CommonModule, RouterModule, Narvar],
  templateUrl: './publicidad.html',
  styleUrl: './publicidad.css'
})
export class PublicidadComponent implements OnInit {
  private publicidadService = inject(PublicidadService);
  private cdr = inject(ChangeDetectorRef);

  publicidades: Publicidad[] = [];

  ngOnInit(): void {
    this.publicidadService.getPublicidades().subscribe(data => {
      this.publicidades = data;
      this.cdr.detectChanges();
    });
  }
}
