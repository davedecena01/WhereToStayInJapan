import { Routes } from '@angular/router';
import { ItineraryInputComponent } from './features/itinerary/itinerary-input/itinerary-input.component';
import { ItineraryReviewComponent } from './features/itinerary/itinerary-review/itinerary-review.component';
import { ResultsComponent } from './features/results/results/results.component';

export const routes: Routes = [
  { path: '', component: ItineraryInputComponent },
  { path: 'review', component: ItineraryReviewComponent },
  { path: 'results', component: ResultsComponent },
  { path: '**', redirectTo: '' }
];
