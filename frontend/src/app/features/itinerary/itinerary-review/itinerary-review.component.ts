import { Component, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Router } from '@angular/router';
import { ItineraryStore } from '../../../core/stores/itinerary.store';
import { SessionService } from '../../../core/services/session.service';
import { RecommendationStore } from '../../../core/stores/recommendation.store';
import { ItineraryChatComponent } from '../itinerary-chat/itinerary-chat.component';
import { ChatItinerary, ParsedItinerary } from '../../../core/models/itinerary.models';

@Component({
  selector: 'app-itinerary-review',
  imports: [CommonModule, RouterLink, ItineraryChatComponent],
  templateUrl: './itinerary-review.component.html',
  styleUrl: './itinerary-review.component.scss'
})
export class ItineraryReviewComponent {
  private readonly router = inject(Router);
  private readonly session = inject(SessionService);
  private readonly recStore = inject(RecommendationStore);

  readonly store = inject(ItineraryStore);
  readonly itinerary = this.store.parsedItinerary;
  readonly isLowConfidence = this.store.isLowConfidence;
  readonly isMultiRegion = this.store.isMultiRegion;

  /** Converts the snake_case store model to the camelCase shape the chat component expects */
  readonly chatItinerary = computed<ChatItinerary | null>(() => {
    const parsed = this.itinerary();
    if (!parsed) return null;
    return {
      destinations: parsed.destinations.map(d => ({
        name: d.name,
        city: d.city,
        region: d.region,
        dayNumber: d.day_number,
        activityType: d.activity_type,
        lat: d.geo_point?.lat ?? null,
        lng: d.geo_point?.lng ?? null,
        isAmbiguous: false
      })),
      regionsDetected: parsed.regions_detected,
      isMultiRegion: parsed.is_multi_region,
      startDate: parsed.travel_dates?.start ?? null,
      endDate: parsed.travel_dates?.end ?? null,
      parsingConfidence: parsed.parsing_confidence,
      clarificationNeeded: parsed.clarification_needed,
      rawText: null
    };
  });

  confirmAndContinue(): void {
    const itinerary = this.itinerary();
    const preferences = this.store.userPreferences();
    if (!itinerary) return;

    this.session.save(itinerary, preferences);

    // Kick off recommendation fetch and navigate — results page shows loading state
    this.recStore.fetchRecommendations(itinerary, preferences);
    this.router.navigate(['/results']);
  }

  onItineraryAccepted(updated: ParsedItinerary): void {
    // Store already updated in the chat component — just scroll to top
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  goBack(): void {
    this.router.navigate(['/']);
  }
}
