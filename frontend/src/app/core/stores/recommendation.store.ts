import { computed, inject, Injectable, signal } from '@angular/core';
import { firstValueFrom, TimeoutError } from 'rxjs';
import { ApiService } from '../services/api.service';
import { RecommendationResult, StayAreaRecommendation, UserPreferences, ParsedItinerary } from '../models/itinerary.models';

const LOADING_MESSAGES = [
  'Finding the best areas for your itinerary…',
  'Geocoding your destinations…',
  'Calculating travel times from candidate areas…',
  'Scoring and ranking stay areas…',
  'Fetching hotel options for top areas…',
  'Still working — routing calculations can take a moment on first use…',
];

@Injectable({ providedIn: 'root' })
export class RecommendationStore {
  private readonly api = inject(ApiService);

  readonly result = signal<RecommendationResult | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly isTimedOut = signal(false);
  readonly loadingMessage = signal(LOADING_MESSAGES[0]);

  readonly recommendations = computed(() => this.result()?.recommendations ?? []);
  readonly isMultiRegion = computed(() => this.result()?.is_multi_region ?? false);
  readonly multiRegionWarning = computed(() => this.result()?.multi_region_warning ?? null);
  readonly hasResults = computed(() => this.recommendations().length > 0);

  private _lastItinerary: ParsedItinerary | null = null;
  private _lastPreferences: UserPreferences | null = null;
  private _messageTimer: ReturnType<typeof setInterval> | null = null;

  async fetchRecommendations(itinerary: ParsedItinerary, preferences: UserPreferences): Promise<void> {
    this._lastItinerary = itinerary;
    this._lastPreferences = preferences;

    this.loading.set(true);
    this.error.set(null);
    this.isTimedOut.set(false);
    this._startProgressMessages();

    try {
      const result = await firstValueFrom(this.api.getRecommendations({ itinerary, preferences }));
      this.result.set(result);
    } catch (err) {
      if (err instanceof TimeoutError) {
        this.isTimedOut.set(true);
        this.error.set(
          'The request timed out — routing calculations can be slow on first use. Please try again.'
        );
      } else {
        this.error.set('Unable to load recommendations. Please try again.');
      }
    } finally {
      this.loading.set(false);
      this._stopProgressMessages();
    }
  }

  async retry(): Promise<void> {
    if (this._lastItinerary && this._lastPreferences) {
      await this.fetchRecommendations(this._lastItinerary, this._lastPreferences);
    }
  }

  clear(): void {
    this.result.set(null);
    this.error.set(null);
    this.loading.set(false);
    this.isTimedOut.set(false);
    this.loadingMessage.set(LOADING_MESSAGES[0]);
    this._stopProgressMessages();
  }

  getByRank(rank: number): StayAreaRecommendation | undefined {
    return this.recommendations().find(r => r.rank === rank);
  }

  private _startProgressMessages(): void {
    this._stopProgressMessages();
    let idx = 0;
    this.loadingMessage.set(LOADING_MESSAGES[0]);
    this._messageTimer = setInterval(() => {
      idx = Math.min(idx + 1, LOADING_MESSAGES.length - 1);
      this.loadingMessage.set(LOADING_MESSAGES[idx]);
    }, 15_000);
  }

  private _stopProgressMessages(): void {
    if (this._messageTimer !== null) {
      clearInterval(this._messageTimer);
      this._messageTimer = null;
    }
  }
}
