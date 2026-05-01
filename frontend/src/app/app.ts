import { Component, computed, inject, signal } from '@angular/core';
import { RouterOutlet, RouterLink } from '@angular/router';
import { SessionService } from './core/services/session.service';
import { ItineraryStore } from './core/stores/itinerary.store';
import { Router } from '@angular/router';

interface SakuraPetal {
  id: number;
  left: number;
  duration: number;
  delay: number;
  size: number;
  sway: number;
  opacity: number;
}

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  private readonly router = inject(Router);

  readonly session = inject(SessionService);
  readonly store = inject(ItineraryStore);

  private readonly bannerDismissed = signal(false);
  readonly showResumeBanner = computed(() => this.session.hasActiveSession() && !this.bannerDismissed());

  readonly petals: SakuraPetal[] = Array.from({ length: 22 }, (_, i) => ({
    id: i,
    left: Math.random() * 100,
    duration: 4 + Math.random() * 5,
    delay: Math.random() * 8,
    size: 6 + Math.random() * 7,
    sway: (Math.random() - 0.5) * 80,
    opacity: 0.35 + Math.random() * 0.45,
  }));

  dismissResumeBanner(): void {
    this.bannerDismissed.set(true);
  }

  resumeSession(): void {
    const data = this.session.load();
    if (!data?.parsedItinerary) return;
    this.store.setItinerary(data.parsedItinerary);
    if (data.preferences) this.store.updatePreferences(data.preferences);
    this.router.navigate(['/review']);
  }
}
