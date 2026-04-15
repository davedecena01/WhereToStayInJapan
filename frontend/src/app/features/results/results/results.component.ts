import { Component, inject, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { NgClass } from '@angular/common';
import { RecommendationStore } from '../../../core/stores/recommendation.store';
import { ItineraryStore } from '../../../core/stores/itinerary.store';
import { StayAreaRecommendation } from '../../../core/models/itinerary.models';

@Component({
  selector: 'app-results',
  standalone: true,
  imports: [RouterLink, NgClass],
  templateUrl: './results.component.html',
  styleUrl: './results.component.scss'
})
export class ResultsComponent implements OnInit {
  private readonly router = inject(Router);
  protected readonly recStore = inject(RecommendationStore);
  protected readonly itinStore = inject(ItineraryStore);

  ngOnInit(): void {
    if (!this.recStore.hasResults() && !this.recStore.loading()) {
      // Guard: if no results and not loading, go back to start
      if (!this.itinStore.hasItinerary()) {
        this.router.navigate(['/']);
      }
    }
  }

  retryRequest(): void {
    this.recStore.retry();
  }

  startOver(): void {
    this.itinStore.reset();
    this.recStore.clear();
    this.router.navigate(['/']);
  }

  scorePercent(score: number): number {
    return Math.round(score * 100);
  }

  rankLabel(rank: number): string {
    const labels: Record<number, string> = { 1: '1st', 2: '2nd', 3: '3rd' };
    return labels[rank] ?? `${rank}th`;
  }

  formatPrice(jpy: number): string {
    return `¥${jpy.toLocaleString()}`;
  }

  trackByAreaId(_: number, rec: StayAreaRecommendation): string {
    return rec.area_id;
  }
}
