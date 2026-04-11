import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { ItineraryStore } from '../../../core/stores/itinerary.store';
import { HotelItem, HotelSearchResult } from '../../../core/models/itinerary.models';
import { HotelCardComponent } from '../hotel-card/hotel-card.component';

@Component({
  selector: 'app-hotel-list',
  imports: [RouterLink, HotelCardComponent],
  templateUrl: './hotel-list.component.html',
  styleUrl: './hotel-list.component.scss'
})
export class HotelListComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(ApiService);
  private readonly itinStore = inject(ItineraryStore);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly result = signal<HotelSearchResult | null>(null);
  readonly currentPage = signal(1);
  readonly areaName = signal('');

  private areaId = '';

  ngOnInit(): void {
    this.areaId = this.route.snapshot.paramMap.get('areaId') ?? '';
    this.areaName.set(this.route.snapshot.queryParamMap.get('area') ?? 'This area');
    this.loadPage(1);
  }

  loadPage(page: number): void {
    const prefs = this.itinStore.userPreferences();
    this.loading.set(true);
    this.error.set(null);
    this.currentPage.set(page);

    this.api.getHotels(
      this.areaId,
      prefs?.budget_tier ?? 'mid',
      prefs?.checkin ?? '',
      prefs?.checkout ?? '',
      prefs?.travelers ?? 1,
      page
    ).subscribe({
      next: res => {
        this.result.set(res);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load hotels. Please try again.');
        this.loading.set(false);
      }
    });
  }

  onHotelClick(hotel: HotelItem): void {
    this.api.trackHotelClick({
      session_id: `guest-${Date.now()}`,
      hotel_id: hotel.hotel_id,
      area_id: this.areaId
    });
  }
}
