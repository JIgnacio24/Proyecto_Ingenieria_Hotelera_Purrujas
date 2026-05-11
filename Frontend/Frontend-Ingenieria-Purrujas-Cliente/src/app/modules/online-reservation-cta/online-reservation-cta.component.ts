import { Component, Input } from '@angular/core';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-online-reservation-cta',
  standalone: true,
  imports: [RouterModule],
  templateUrl: './online-reservation-cta.component.html',
  styleUrl: './online-reservation-cta.component.css'
})
export class OnlineReservationCtaComponent {
  @Input() tag = 'Tu estancia';
  @Input() title = 'Reserva en línea';
  @Input() description = 'Confirma tu habitación en minutos. Pagos seguros y cancelación flexible hasta 7 días antes.';
  @Input() ctaText = 'Solicitar disponibilidad';
  @Input() ctaLink = '/reservar';
}
