import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ParsedItinerary } from '../models/itinerary.models';
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
}
