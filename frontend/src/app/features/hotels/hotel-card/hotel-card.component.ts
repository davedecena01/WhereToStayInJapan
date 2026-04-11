import { Component, input, output } from '@angular/core';
import { HotelItem } from '../../../core/models/itinerary.models';

@Component({
  selector: 'app-hotel-card',
  imports: [],
  templateUrl: './hotel-card.component.html',
  styleUrl: './hotel-card.component.scss'
})
export class HotelCardComponent {
  readonly hotel = input.required<HotelItem>();
  readonly clicked = output<HotelItem>();

  formatPrice(jpy: number): string {
    return `¥${jpy.toLocaleString()}`;
  }

  onBookClick(): void {
    this.clicked.emit(this.hotel());
    window.open(this.hotel().deep_link_url, '_blank', 'noopener,noreferrer');
  }
}
