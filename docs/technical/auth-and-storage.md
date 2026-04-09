# Auth and Storage — Where To Stay In Japan

---

## V1: Guest Mode Only

No authentication in V1. No user accounts. No server-side user data.

Users interact as anonymous guests. Their session is stored entirely in browser `localStorage`. Nothing personally identifiable is sent to the server — the `session_id` is a randomly generated UUID with no link to any identity.

---

## localStorage Schema

**Key:** `wtsjp_session`

```typescript
interface WtsjpSession {
  version: '1.0';
  session_id: string;        // UUID v4 — generated on first visit, never changes
  created_at: string;        // ISO 8601, set once on session creation
  expires_at: string;        // created_at + 30 days
  last_updated: string;      // ISO 8601, updated on every save
  itinerary: ParsedItinerary | null;
  preferences: UserPreferences | null;
  recommendations: RecommendationResult[] | null;
}
```

**TTL:** 30 days from `created_at`. Checked on every `loadSession()` call.
**Size limit:** localStorage is typically 5–10MB per origin. A full session with 5 recommendations should be ~50–100KB — well within limits.

---

## `SessionService`

Single service for all localStorage access. No component touches `localStorage` directly.

```typescript
// src/app/shared/services/session.service.ts
@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly STORAGE_KEY = 'wtsjp_session';
  private readonly SESSION_TTL_DAYS = 30;

  private _session = signal<WtsjpSession | null>(null);

  // Computed signals for template use
  readonly hasSession = computed(() => this._session() !== null);
  readonly sessionItinerary = computed(() => this._session()?.itinerary ?? null);
  readonly sessionPreferences = computed(() => this._session()?.preferences ?? null);
  readonly sessionRecommendations = computed(() => this._session()?.recommendations ?? null);

  constructor(@Inject(PLATFORM_ID) private platformId: object) {
    if (isPlatformBrowser(this.platformId)) {
      this._session.set(this.loadFromStorage());
    }
  }

  loadSession(): WtsjpSession | null {
    return this._session();
  }

  saveSession(partial: Partial<WtsjpSession>): void {
    if (!isPlatformBrowser(this.platformId)) return;

    const current = this._session() ?? this.createNewSession();
    const updated: WtsjpSession = {
      ...current,
      ...partial,
      last_updated: new Date().toISOString()
    };
    try {
      localStorage.setItem(this.STORAGE_KEY, JSON.stringify(updated));
      this._session.set(updated);
    } catch (err) {
      // localStorage may be unavailable (private browsing quotas, storage full)
      console.warn('Session save failed:', err);
    }
  }

  clearSession(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    localStorage.removeItem(this.STORAGE_KEY);
    this._session.set(null);
  }

  private loadFromStorage(): WtsjpSession | null {
    try {
      const raw = localStorage.getItem(this.STORAGE_KEY);
      if (!raw) return null;

      const session: WtsjpSession = JSON.parse(raw);

      // Version check
      if (session.version !== '1.0') {
        this.clearSession();
        return null;
      }

      // TTL check
      if (new Date(session.expires_at) <= new Date()) {
        this.clearSession();
        return null;
      }

      return session;
    } catch {
      // Corrupted localStorage data
      this.clearSession();
      return null;
    }
  }

  private createNewSession(): WtsjpSession {
    const now = new Date();
    const expires = new Date(now);
    expires.setDate(expires.getDate() + this.SESSION_TTL_DAYS);

    return {
      version: '1.0',
      session_id: crypto.randomUUID(),
      created_at: now.toISOString(),
      expires_at: expires.toISOString(),
      last_updated: now.toISOString(),
      itinerary: null,
      preferences: null,
      recommendations: null
    };
  }
}
```

**`isPlatformBrowser` guard:** Required for future SSR compatibility. When Angular renders server-side, `localStorage` doesn't exist. Guards prevent server-side crashes.

---

## Session Restore UX

The root `AppComponent` checks for a saved session on init and shows a restoration banner if one exists.

```typescript
// app.component.ts
@Component({ ... })
export class AppComponent {
  readonly sessionService = inject(SessionService);
  readonly showRestoreBanner = this.sessionService.hasSession;

  continuePreviousSession(): void {
    // Session is already loaded — navigate directly to results
    this.router.navigate(['/results']);
  }

  startFresh(): void {
    this.sessionService.clearSession();
    // Banner disappears via hasSession signal
  }
}
```

```html
<!-- app.component.html -->
@if (showRestoreBanner()) {
  <div class="session-restore-banner" role="alert">
    <span>Welcome back! You have a saved trip.</span>
    <button (click)="continuePreviousSession()">Continue session</button>
    <button (click)="startFresh()">Start fresh</button>
  </div>
}
<router-outlet />
```

---

## What Is and Is Not Sent to the Server

**Sent to server (in API requests):**
- `session_id` (UUID — no personal data)
- `ParsedItinerary` (place names, travel dates — not personally identifiable)
- `UserPreferences` (travel dates, budget, travelers — no identity)
- Hotel click events (`session_id`, `hotel_id`, `area_id`)

**Never sent to server:**
- Raw itinerary text (parsing happens on server, but only for the duration of the parse request; not stored)
- User's name, email, or any identity
- localStorage session data

**Stored on server:**
- `recommendation_logs` (session_id hash, top areas, ai_used, hotels_fetched — no PII)
- `hotel_click_logs` (session_id, hotel_id, area_id, timestamp — no PII)

---

## Privacy Note

All data in localStorage remains on the user's device. The `session_id` is an anonymous UUID — not linked to any identity. The app does not use cookies in V1 (no tracking cookies, no session cookies).

---

## Future Auth Design (Phase 2)

**Why not auth in V1:** Auth adds complexity (token management, refresh logic, protected routes, account management UI) that is not necessary for the core recommendation functionality. Guest mode delivers full value.

**Planned auth approach for Phase 2:**

### Provider: Supabase Auth (built-in, free tier)

Supabase provides authentication out of the box with the free tier. No separate auth service needed.

**Method 1 — Email Magic Link (primary):**
- User enters email → Supabase sends magic link email
- User clicks link → redirected back with token
- Token stored in httpOnly cookie (not localStorage — prevents XSS access)
- Backend validates token via Supabase JWT verification

**Method 2 — Google OAuth (Phase 3 optional):**
- Standard OAuth2 flow via Supabase
- Add after magic link is stable

### Session Migration on First Login

When a guest user logs in for the first time:
1. Their localStorage session (`WtsjpSession`) is sent to `POST /api/account/migrate-session`
2. Backend creates a server-side user record and migrates itinerary/recommendations
3. localStorage session is cleared (data now server-side)
4. Future sessions are server-side only (no localStorage dependency post-auth)

### Post-Auth Token Storage

```
V1 (guest): localStorage only
Phase 2 (authenticated): httpOnly cookie (SameSite=Strict)
```

Never store JWT in localStorage — XSS vulnerability. Use httpOnly cookies set by the API on login.

### Protected Routes (Phase 2)

Add Angular route guard: `AuthGuard` that checks `AuthService.isAuthenticated()`. Routes `/account`, `/saved-trips` become protected. All current routes (`/`, `/results`, `/area/:id`) remain public.

---

## Backend Storage (V1)

The backend stores only:
- Cache tables (geocode, routing, AI responses, hotel searches) — no user data, keyed by content hash
- `recommendation_logs` — anonymous, keyed by session_id UUID
- `hotel_click_logs` — anonymous, keyed by session_id UUID

**No user table exists in V1.** The first EF Core migration that introduces a `users` table belongs to Phase 2.

---

## Handling localStorage Unavailability

`localStorage` can be unavailable in:
- Private/incognito mode (some browsers restrict quota)
- Storage quota exceeded
- Browser settings blocking storage
- `localStorage` access throws `SecurityError` in some contexts

The `SessionService` wraps all `localStorage` calls in try/catch. If write fails, the app continues working — session is held in memory for the current page load only. User sees no error.

```typescript
// Graceful degradation
try {
  localStorage.setItem(this.STORAGE_KEY, JSON.stringify(updated));
} catch {
  // Silent fail — session still in memory via signal
  // No crash, no error shown to user
}
```
