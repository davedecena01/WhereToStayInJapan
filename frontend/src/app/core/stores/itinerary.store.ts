import { computed, Injectable, signal } from '@angular/core';
import { ParsedItinerary, UserPreferences } from '../models/itinerary.models';

const DEFAULT_PREFERENCES: UserPreferences = {
  check_in: '',
  check_out: '',
  travelers: 2,
  budget_tier: 'mid',
  avoid_long_walking: false,
  must_be_near_station: null,
  preferred_atmosphere: []
};

@Injectable({ providedIn: 'root' })
export class ItineraryStore {
  readonly parsedItinerary = signal<ParsedItinerary | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly userPreferences = signal<UserPreferences>({ ...DEFAULT_PREFERENCES });

  readonly hasItinerary = computed(() => this.parsedItinerary() !== null);
  readonly isMultiRegion = computed(() => this.parsedItinerary()?.is_multi_region ?? false);
  readonly isLowConfidence = computed(() => this.parsedItinerary()?.parsing_confidence === 'low');

  setItinerary(itinerary: ParsedItinerary): void {
    this.parsedItinerary.set(itinerary);
    this.error.set(null);
  }

  setLoading(loading: boolean): void {
    this.loading.set(loading);
  }

  setError(message: string | null): void {
    this.error.set(message);
  }

  updatePreferences(patch: Partial<UserPreferences>): void {
    this.userPreferences.update(prefs => ({ ...prefs, ...patch }));
  }

  reset(): void {
    this.parsedItinerary.set(null);
    this.error.set(null);
    this.loading.set(false);
    this.userPreferences.set({ ...DEFAULT_PREFERENCES });
  }
}
