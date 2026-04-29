import { Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs/operators';
import { ApiService } from '../../../core/services/api.service';
import { ItineraryStore } from '../../../core/stores/itinerary.store';
import { BudgetTier, GenerationMode, Pace, TravelStyle } from '../../../core/models/itinerary.models';

const DURATION_MAP: Record<string, number> = {
  '3–5 days': 4,
  '5–7 days': 6,
  '7–10 days': 8,
  '10+ days': 12
};

const DURATION_LABELS = ['3–5 days', '5–7 days', '7–10 days', '10+ days'];
const REGIONS = ['Kanto', 'Kansai', 'Chubu', 'Kyushu', 'Tohoku', 'Hokkaido', 'Okinawa'];

const STYLE_OPTIONS: { value: TravelStyle; label: string }[] = [
  { value: 'cultural', label: 'Cultural' },
  { value: 'foodie',   label: 'Foodie' },
  { value: 'nature',   label: 'Nature' },
  { value: 'urban',    label: 'Urban' },
  { value: 'mix',      label: 'Mix' }
];

const PACE_OPTIONS: { value: Pace; label: string }[] = [
  { value: 'relaxed',  label: 'Relaxed' },
  { value: 'moderate', label: 'Moderate' },
  { value: 'packed',   label: 'Packed' }
];

const BUDGET_OPTIONS: { value: BudgetTier; label: string }[] = [
  { value: 'budget',  label: 'Budget' },
  { value: 'mid',     label: 'Mid-range' },
  { value: 'luxury',  label: 'Luxury' }
];

@Component({
  selector: 'app-itinerary-create',
  imports: [CommonModule],
  templateUrl: './itinerary-create.component.html',
  styleUrl: './itinerary-create.component.scss'
})
export class ItineraryCreateComponent {
  private readonly api = inject(ApiService);
  private readonly store = inject(ItineraryStore);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  // Exposed options for template
  readonly durationLabels = DURATION_LABELS;
  readonly regions = REGIONS;
  readonly styleOptions = STYLE_OPTIONS;
  readonly paceOptions = PACE_OPTIONS;
  readonly budgetOptions = BUDGET_OPTIONS;

  // Standard mode state
  readonly selectedDuration = signal<number | null>(null);
  readonly selectedRegions = signal<string[]>([]);
  readonly selectedStyle = signal<TravelStyle | null>(null);
  readonly selectedPace = signal<Pace | null>(null);
  readonly selectedBudget = signal<BudgetTier | null>(null);

  // Challenge mode state
  readonly challengeDuration = signal<number | null>(null);
  readonly challengeRegions = signal<string[]>([]);

  // Shared state
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  // Computed readiness
  readonly standardReady = computed(() =>
    this.selectedDuration() !== null &&
    this.selectedRegions().length > 0 &&
    this.selectedStyle() !== null &&
    this.selectedPace() !== null &&
    this.selectedBudget() !== null
  );

  readonly challengeReady = computed(() =>
    this.challengeDuration() !== null &&
    this.challengeRegions().length > 0
  );

  // Step indicator: count how many of the 4 standard steps have a selection
  readonly currentStep = computed(() => {
    let step = 1;
    if (this.selectedDuration() !== null) step = 2;
    if (this.selectedRegions().length > 0) step = 3;
    if (this.selectedStyle() !== null) step = 4;
    return step;
  });

  selectDuration(label: string): void {
    this.selectedDuration.set(DURATION_MAP[label]);
  }

  selectChallengeDuration(label: string): void {
    this.challengeDuration.set(DURATION_MAP[label]);
  }

  durationLabel(days: number | null): string | null {
    if (days === null) return null;
    return Object.keys(DURATION_MAP).find(k => DURATION_MAP[k] === days) ?? null;
  }

  toggleRegion(region: string, target: 'standard' | 'challenge'): void {
    const sig = target === 'standard' ? this.selectedRegions : this.challengeRegions;
    const current = sig();
    sig.set(current.includes(region)
      ? current.filter(r => r !== region)
      : [...current, region]);
  }

  generateStandard(): void {
    if (!this.standardReady() || this.loading()) return;

    this.error.set(null);
    this.loading.set(true);

    this.api.generateItinerary({
      mode: 'standard' as GenerationMode,
      duration_days: this.selectedDuration()!,
      regions: this.selectedRegions(),
      travel_style: this.selectedStyle()!,
      budget_tier: this.selectedBudget()!,
      pace: this.selectedPace()!
    })
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: itinerary => {
          this.store.setItinerary(itinerary);
          this.router.navigate(['/review']);
        },
        error: () => this.error.set('Generation failed. Please try again.')
      });
  }

  generateChallenge(): void {
    if (!this.challengeReady() || this.loading()) return;

    this.error.set(null);
    this.loading.set(true);

    this.api.generateItinerary({
      mode: 'challenge' as GenerationMode,
      duration_days: this.challengeDuration()!,
      regions: this.challengeRegions()
    })
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: itinerary => {
          this.store.setItinerary(itinerary);
          this.router.navigate(['/review']);
        },
        error: () => this.error.set('Generation failed. Please try again.')
      });
  }
}
