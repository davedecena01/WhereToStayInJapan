import { computed, inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../services/api.service';
import { RecommendationResult, StayAreaRecommendation, UserPreferences, ParsedItinerary } from '../models/itinerary.models';

@Injectable({ providedIn: 'root' })
export class RecommendationStore {
  private readonly api = inject(ApiService);

  readonly result = signal<RecommendationResult | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly recommendations = computed(() => this.result()?.recommendations ?? []);
  readonly isMultiRegion = computed(() => this.result()?.is_multi_region ?? false);
  readonly multiRegionWarning = computed(() => this.result()?.multi_region_warning ?? null);
  readonly hasResults = computed(() => this.recommendations().length > 0);

  async fetchRecommendations(itinerary: ParsedItinerary, preferences: UserPreferences): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const result = await firstValueFrom(this.api.getRecommendations({ itinerary, preferences }));
      this.result.set(result);
    } catch {
      this.error.set('Unable to load recommendations. Please try again.');
    } finally {
      this.loading.set(false);
    }
  }

  clear(): void {
    this.result.set(null);
    this.error.set(null);
    this.loading.set(false);
  }

  getByRank(rank: number): StayAreaRecommendation | undefined {
    return this.recommendations().find(r => r.rank === rank);
  }
}
