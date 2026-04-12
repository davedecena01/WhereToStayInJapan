import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ChatItinerary, ChatResponse, HotelClickRequest, HotelSearchResult, ParsedItinerary, RecommendationRequest, RecommendationResult } from '../models/itinerary.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  parseText(text: string): Observable<ParsedItinerary> {
    return this.http.post<ParsedItinerary>(`${this.base}/api/itinerary/parse`, { text });
  }

  parseFile(file: File): Observable<ParsedItinerary> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<ParsedItinerary>(`${this.base}/api/itinerary/parse/file`, form);
  }

  getRecommendations(request: RecommendationRequest): Observable<RecommendationResult> {
    return this.http.post<RecommendationResult>(`${this.base}/api/recommendations`, request);
  }

  sendChatMessage(sessionId: string, message: string, currentItinerary: ChatItinerary | null): Observable<ChatResponse> {
    return this.http.post<ChatResponse>(`${this.base}/api/chat`, {
      session_id: sessionId,
      message,
      current_itinerary: currentItinerary
    });
  }

  getHotels(areaId: string, budgetTier: string, checkin: string, checkout: string, travelers: number, page = 1): Observable<HotelSearchResult> {
    const params = new URLSearchParams({
      area_id: areaId,
      budget_tier: budgetTier,
      checkin,
      checkout,
      travelers: travelers.toString(),
      page: page.toString()
    });
    return this.http.get<HotelSearchResult>(`${this.base}/api/hotels?${params}`);
  }

  trackHotelClick(req: HotelClickRequest): void {
    this.http.post(`${this.base}/api/analytics/hotel-click`, req).subscribe({ error: () => {} });
  }
}
