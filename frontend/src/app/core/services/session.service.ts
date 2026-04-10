import { Injectable, signal } from '@angular/core';
import { ParsedItinerary, UserPreferences } from '../models/itinerary.models';

interface SessionData {
  version: number;
  savedAt: string; // ISO datetime
  preferences: UserPreferences | null;
  parsedItinerary: ParsedItinerary | null;
}

const SESSION_KEY = 'wtsjp_session';
const SESSION_VERSION = 1;
const TTL_DAYS = 7;

@Injectable({ providedIn: 'root' })
export class SessionService {
  readonly hasActiveSession = signal(false);

  constructor() {
    this.hasActiveSession.set(this.checkActiveSession());
  }

  save(itinerary: ParsedItinerary, preferences: UserPreferences): void {
    const data: SessionData = {
      version: SESSION_VERSION,
      savedAt: new Date().toISOString(),
      preferences,
      parsedItinerary: itinerary
    };
    localStorage.setItem(SESSION_KEY, JSON.stringify(data));
    this.hasActiveSession.set(true);
  }

  load(): SessionData | null {
    const raw = localStorage.getItem(SESSION_KEY);
    if (!raw) return null;

    try {
      const data = JSON.parse(raw) as SessionData;
      if (data.version !== SESSION_VERSION) {
        this.clear();
        return null;
      }
      if (this.isExpired(data.savedAt)) {
        this.clear();
        return null;
      }
      return data;
    } catch {
      this.clear();
      return null;
    }
  }

  clear(): void {
    localStorage.removeItem(SESSION_KEY);
    this.hasActiveSession.set(false);
  }

  private checkActiveSession(): boolean {
    const data = this.load();
    return data !== null && data.parsedItinerary !== null;
  }

  private isExpired(savedAt: string): boolean {
    const saved = new Date(savedAt);
    const cutoff = new Date();
    cutoff.setDate(cutoff.getDate() - TTL_DAYS);
    return saved < cutoff;
  }
}
