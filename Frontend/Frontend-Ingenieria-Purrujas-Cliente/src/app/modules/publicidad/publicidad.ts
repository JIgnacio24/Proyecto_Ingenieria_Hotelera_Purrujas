import { Component, OnInit, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ChangeDetectorRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { Publicidad, PublicidadService } from '../../services/publicidad.service';

@Component({
  selector: 'app-publicidad',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './publicidad.html',
  styleUrl: './publicidad.css'
})
export class PublicidadComponent implements OnInit {
  private publicidadService = inject(PublicidadService);
  private cdr = inject(ChangeDetectorRef);
  private destroyRef = inject(DestroyRef);

  publicidades: Publicidad[] = [];
  publicidadesLoading = true;

  ngOnInit(): void {
    this.publicidadService.getPublicidades().pipe(
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(data => {
      this.publicidades = data;
      this.publicidadesLoading = false;
      this.cdr.detectChanges();
    });
  }
}
