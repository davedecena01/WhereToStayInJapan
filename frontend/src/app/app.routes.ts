import { Routes } from '@angular/router';
import { ItineraryInputComponent } from './features/itinerary/itinerary-input/itinerary-input.component';
import { ItineraryReviewComponent } from './features/itinerary/itinerary-review/itinerary-review.component';
import { ResultsComponent } from './features/results/results/results.component';
import { HotelListComponent } from './features/hotels/hotel-list/hotel-list.component';

export const routes: Routes = [
  { path: '', component: ItineraryInputComponent },
  { path: 'review', component: ItineraryReviewComponent },
  { path: 'results', component: ResultsComponent },
  { path: 'hotels/:areaId', component: HotelListComponent },
  { path: '**', redirectTo: '' }
];
