import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { ItineraryStore } from '../../../core/stores/itinerary.store';
import { SessionService } from '../../../core/services/session.service';

@Component({
  selector: 'app-itinerary-review',
  imports: [CommonModule],
  templateUrl: './itinerary-review.component.html',
  styleUrl: './itinerary-review.component.scss'
})
export class ItineraryReviewComponent {
  private readonly router = inject(Router);
  private readonly session = inject(SessionService);

  readonly store = inject(ItineraryStore);
  readonly itinerary = this.store.parsedItinerary;
  readonly isLowConfidence = this.store.isLowConfidence;
  readonly isMultiRegion = this.store.isMultiRegion;

  confirmAndContinue(): void {
    const itinerary = this.itinerary();
    const preferences = this.store.userPreferences();
    if (!itinerary) return;

    this.session.save(itinerary, preferences);
    this.router.navigate(['/results']);
  }

  goBack(): void {
    this.router.navigate(['/']);
  }
}
