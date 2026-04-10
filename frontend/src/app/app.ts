import { Component, inject } from '@angular/core';
import { RouterOutlet, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { SessionService } from './core/services/session.service';
import { ItineraryStore } from './core/stores/itinerary.store';
import { Router } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, CommonModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  private readonly router = inject(Router);

  readonly session = inject(SessionService);
  readonly store = inject(ItineraryStore);

  readonly showResumeBanner = this.session.hasActiveSession;

  dismissResumeBanner(): void {
    this.session.clear();
  }

  resumeSession(): void {
    const data = this.session.load();
    if (!data?.parsedItinerary) return;
    this.store.setItinerary(data.parsedItinerary);
    if (data.preferences) this.store.updatePreferences(data.preferences);
    this.router.navigate(['/review']);
  }
}
