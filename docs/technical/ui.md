# UI — Where To Stay In Japan

Framework: Angular 17+ (standalone components, no NgModules)
State: Angular signals + injectable store services (no NgRx)
Styling: SCSS with custom variables, no UI component library required
Build: Angular CLI, `ng build --configuration production`
Deployment: Static files on Vercel

---

## Project Structure

```
src/
├── app/
│   ├── features/
│   │   ├── itinerary/
│   │   │   ├── itinerary-input/
│   │   │   │   ├── itinerary-input.component.ts
│   │   │   │   ├── itinerary-input.component.html
│   │   │   │   └── itinerary-input.component.scss
│   │   │   ├── itinerary-review/
│   │   │   │   ├── itinerary-review.component.ts
│   │   │   │   ├── itinerary-review.component.html
│   │   │   │   └── itinerary-review.component.scss
│   │   │   ├── itinerary-chat/
│   │   │   │   ├── itinerary-chat.component.ts
│   │   │   │   ├── itinerary-chat.component.html
│   │   │   │   └── itinerary-chat.component.scss
│   │   │   └── itinerary.store.ts
│   │   │
│   │   ├── recommendations/
│   │   │   ├── recommendation-list/
│   │   │   │   ├── recommendation-list.component.ts
│   │   │   │   ├── recommendation-list.component.html
│   │   │   │   └── recommendation-list.component.scss
│   │   │   ├── recommendation-card/
│   │   │   │   ├── recommendation-card.component.ts
│   │   │   │   ├── recommendation-card.component.html
│   │   │   │   └── recommendation-card.component.scss
│   │   │   ├── recommendation-detail/
│   │   │   │   ├── recommendation-detail.component.ts
│   │   │   │   ├── recommendation-detail.component.html
│   │   │   │   └── recommendation-detail.component.scss
│   │   │   └── recommendations.store.ts
│   │   │
│   │   └── hotels/
│   │       ├── hotel-list/
│   │       │   ├── hotel-list.component.ts
│   │       │   └── hotel-list.component.html
│   │       ├── hotel-card/
│   │       │   ├── hotel-card.component.ts
│   │       │   └── hotel-card.component.html
│   │       └── hotels.store.ts
│   │
│   ├── shared/
│   │   ├── components/
│   │   │   ├── loading-spinner/
│   │   │   ├── error-banner/
│   │   │   ├── empty-state/
│   │   │   ├── skeleton-card/
│   │   │   └── preference-form/        -- reusable travel prefs form
│   │   ├── services/
│   │   │   ├── api.service.ts
│   │   │   └── session.service.ts
│   │   └── models/
│   │       └── index.ts                -- re-exports all TypeScript interfaces
│   │
│   ├── app.component.ts                -- root component, session restore banner
│   ├── app.component.html
│   ├── app.routes.ts
│   └── app.config.ts                   -- provideRouter, provideHttpClient, etc.
│
├── styles/
│   ├── _variables.scss
│   ├── _typography.scss
│   ├── _mixins.scss
│   └── _reset.scss
│
└── environments/
    ├── environment.ts
    └── environment.production.ts
```

---

## Routing

```typescript
// app.routes.ts
export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/itinerary/itinerary-input/itinerary-input.component')
  },
  {
    path: 'results',
    loadComponent: () => import('./features/recommendations/recommendation-list/recommendation-list.component')
  },
  {
    path: 'area/:id',
    loadComponent: () => import('./features/recommendations/recommendation-detail/recommendation-detail.component')
  },
  {
    path: '**',
    redirectTo: ''
  }
];
```

3 routes total. All lazy-loaded. No route guards in V1.

---

## Signal-Based Store Pattern

Each feature has one injectable store service. Components read signals and call store methods.

**`itinerary.store.ts`:**
```typescript
@Injectable({ providedIn: 'root' })
export class ItineraryStore {
  // State signals
  readonly itinerary = signal<ParsedItinerary | null>(null);
  readonly preferences = signal<UserPreferences | null>(null);
  readonly isParsingLoading = signal(false);
  readonly parsingError = signal<string | null>(null);

  // Derived signals
  readonly hasItinerary = computed(() => this.itinerary() !== null);
  readonly isMultiRegion = computed(() => this.itinerary()?.is_multi_region ?? false);
  readonly regionsDetected = computed(() => this.itinerary()?.regions_detected ?? []);

  constructor(
    private api: ApiService,
    private session: SessionService
  ) {
    // Restore from session on init
    const saved = this.session.loadSession();
    if (saved?.itinerary) this.itinerary.set(saved.itinerary);
    if (saved?.preferences) this.preferences.set(saved.preferences);
  }

  async parseFile(file: File): Promise<void> {
    this.isParsingLoading.set(true);
    this.parsingError.set(null);
    try {
      const result = await firstValueFrom(this.api.parseFile(file));
      this.itinerary.set(result);
      this.session.saveSession({ itinerary: result });
    } catch (err) {
      this.parsingError.set(extractErrorMessage(err));
    } finally {
      this.isParsingLoading.set(false);
    }
  }

  async parseText(text: string): Promise<void> { /* same pattern */ }
  setPreferences(prefs: UserPreferences): void {
    this.preferences.set(prefs);
    this.session.saveSession({ preferences: prefs });
  }
}
```

**`recommendations.store.ts`:**
```typescript
@Injectable({ providedIn: 'root' })
export class RecommendationsStore {
  readonly recommendations = signal<RecommendationResult[]>([]);
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly isPartialResult = signal(false);  // true when hotels unavailable

  async fetchRecommendations(itinerary: ParsedItinerary, prefs: UserPreferences): Promise<void> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      const results = await firstValueFrom(this.api.getRecommendations(itinerary, prefs));
      this.recommendations.set(results);
      this.isPartialResult.set(results.some(r => !r.hotels_available));
      this.session.saveSession({ recommendations: results });
    } catch (err) {
      this.error.set(extractErrorMessage(err));
    } finally {
      this.isLoading.set(false);
    }
  }
}
```

---

## `ApiService`

Single point of HTTP communication. No component touches `HttpClient` directly.

```typescript
@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly baseUrl = environment.apiBaseUrl;

  constructor(private http: HttpClient) {}

  parseFile(file: File): Observable<ParsedItinerary> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<ParsedItinerary>(`${this.baseUrl}/api/itinerary/parse`, formData)
      .pipe(catchError(this.handleError));
  }

  parseText(text: string): Observable<ParsedItinerary> {
    return this.http.post<ParsedItinerary>(`${this.baseUrl}/api/itinerary/parse`, { text })
      .pipe(catchError(this.handleError));
  }

  getRecommendations(itinerary: ParsedItinerary, preferences: UserPreferences): Observable<RecommendationResult[]> {
    return this.http.post<RecommendationResult[]>(`${this.baseUrl}/api/recommendations`, { itinerary, preferences })
      .pipe(catchError(this.handleError));
  }

  getHotels(areaId: string, params: HotelSearchParams): Observable<HotelSearchResponse> {
    return this.http.get<HotelSearchResponse>(`${this.baseUrl}/api/hotels`, { params: { area_id: areaId, ...params } })
      .pipe(catchError(this.handleError));
  }

  getAreaFood(areaId: string): Observable<FoodResponse> {
    return this.http.get<FoodResponse>(`${this.baseUrl}/api/areas/${areaId}/food`)
      .pipe(catchError(this.handleError));
  }

  sendChat(sessionId: string, message: string, itinerary?: ParsedItinerary): Observable<ChatResponse> {
    return this.http.post<ChatResponse>(`${this.baseUrl}/api/chat`, { session_id: sessionId, message, context: { itinerary } })
      .pipe(catchError(this.handleError));
  }

  trackHotelClick(event: HotelClickEvent): void {
    // Fire-and-forget
    this.http.post(`${this.baseUrl}/api/analytics/hotel-click`, event).subscribe({ error: () => {} });
  }

  private handleError(error: HttpErrorResponse): Observable<never> {
    const message = error.error?.detail ?? error.message ?? 'An unknown error occurred';
    return throwError(() => new Error(message));
  }
}
```

---

## Visual Theme (SCSS Variables)

```scss
// styles/_variables.scss

// Brand palette
$color-sakura-pink: #F8A7BB;
$color-sakura-light: #FDE8EE;
$color-navy-deep: #1A2B4A;
$color-navy-medium: #2C4A7C;
$color-warm-white: #FFF8F0;
$color-accent-gold: #D4A017;
$color-accent-gold-light: #F5E8B0;

// Semantic colors
$color-success: #4CAF50;
$color-warning: #FF9800;
$color-error: #F44336;
$color-info: #2196F3;

// Text
$color-text-primary: $color-navy-deep;
$color-text-secondary: #6B7A99;
$color-text-on-dark: #FFFFFF;
$color-text-on-pink: $color-navy-deep;

// Typography
$font-family-sans: 'Noto Sans JP', 'Inter', system-ui, sans-serif;
$font-family-display: 'Noto Serif JP', 'Playfair Display', Georgia, serif;
$font-size-base: 16px;
$font-size-sm: 14px;
$font-size-lg: 18px;
$font-size-xl: 24px;
$font-size-2xl: 32px;
$font-size-3xl: 48px;

// Spacing scale
$space-1: 4px;
$space-2: 8px;
$space-3: 12px;
$space-4: 16px;
$space-6: 24px;
$space-8: 32px;
$space-12: 48px;
$space-16: 64px;

// Layout
$max-width-content: 1200px;
$border-radius-sm: 4px;
$border-radius-md: 8px;
$border-radius-lg: 16px;
$border-radius-pill: 999px;

// Shadows
$shadow-card: 0 2px 8px rgba(26, 43, 74, 0.10);
$shadow-card-hover: 0 4px 16px rgba(26, 43, 74, 0.16);

// Breakpoints
$bp-mobile: 375px;
$bp-tablet: 1024px;
$bp-desktop: 1440px;
```

---

## Breakpoint Mixin

```scss
// styles/_mixins.scss
@mixin tablet {
  @media (max-width: #{$bp-tablet}) { @content; }
}

@mixin mobile {
  @media (max-width: 767px) { @content; }
}
```

Design at 1440px first, then `@include tablet` for 1024px adaptations, then `@include mobile` for 375px minimum.

---

## Component Responsibilities

| Component | Responsibility | Inputs | Key Outputs |
|---|---|---|---|
| `ItineraryInputComponent` | File drag-drop + paste form + preferences form | — | emits on submit → calls `ItineraryStore.parseFile/parseText` |
| `ItineraryReviewComponent` | Display parsed destinations for confirmation | `ParsedItinerary` | "Looks Good" → navigate to `/results` |
| `ItineraryChatComponent` | Chat refinement interface | `ParsedItinerary` | Updated `ParsedItinerary` via `ItineraryStore` |
| `RecommendationListComponent` | Ranked grid of cards | `recommendations` signal | — |
| `RecommendationCardComponent` | Single area summary card | `RecommendationResult` | Click → navigate to `/area/:id` |
| `RecommendationDetailComponent` | Full area detail with hotels, food, attractions | `area_id` route param | Hotel click → `ApiService.trackHotelClick()` |
| `HotelListComponent` | Paginated hotel list | `areaId`, `searchParams` | — |
| `HotelCardComponent` | Single hotel with deep-link button | `HotelItem` | "Book on Rakuten" click → `window.open(deep_link_url, '_blank')` |

---

## Loading and Error States

Every async operation must have 3 states: loading, success, error. No silent failures.

**Recommendation cards loading (skeleton):**
```html
<!-- recommendation-list.component.html -->
@if (store.isLoading()) {
  <div class="recommendation-grid">
    @for (n of [1,2,3]; track n) {
      <app-skeleton-card />
    }
  </div>
} @else if (store.error()) {
  <app-error-banner [message]="store.error()!" [retryable]="true" (retry)="retry()" />
} @else {
  <div class="recommendation-grid">
    @for (rec of store.recommendations(); track rec.area_id) {
      <app-recommendation-card [recommendation]="rec" />
    }
  </div>
}
```

**Partial result banner (hotels unavailable):**
```html
@if (store.isPartialResult()) {
  <app-error-banner
    type="warning"
    message="Hotel results are temporarily unavailable. Area recommendations are complete."
    [retryable]="false"
  />
}
```

---

## File Upload Component

```html
<!-- itinerary-input.component.html (upload zone) -->
<div
  class="upload-zone"
  [class.drag-over]="isDragOver()"
  (dragover)="onDragOver($event)"
  (dragleave)="isDragOver.set(false)"
  (drop)="onDrop($event)"
  (click)="fileInput.click()"
  role="button"
  tabindex="0"
  aria-label="Upload itinerary file — PDF, DOCX, or TXT, max 10MB"
  (keydown.enter)="fileInput.click()"
>
  @if (selectedFile()) {
    <span class="file-info">{{ selectedFile()!.name }} ({{ formatFileSize(selectedFile()!.size) }})</span>
  } @else {
    <span>Drop your itinerary here, or click to browse</span>
    <span class="hint">PDF, DOCX, or TXT — max 10 MB</span>
  }
</div>
<input #fileInput type="file" accept=".pdf,.docx,.txt" (change)="onFileSelected($event)" hidden />
```

Client-side validation before upload:
```typescript
onFileSelected(event: Event): void {
  const file = (event.target as HTMLInputElement).files?.[0];
  if (!file) return;
  if (file.size > 10 * 1024 * 1024) {
    this.fileError.set('File is too large. Maximum size is 10 MB.');
    return;
  }
  const allowed = ['application/pdf', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document', 'text/plain'];
  if (!allowed.includes(file.type)) {
    this.fileError.set('Unsupported file type. Please upload a PDF, DOCX, or TXT file.');
    return;
  }
  this.selectedFile.set(file);
  this.fileError.set(null);
}
```

---

## Accessibility Requirements

- All interactive elements have `aria-label` or visible label
- Keyboard navigation: `Tab` moves through form fields, cards, hotel buttons
- Focus management: after navigation to `/results`, focus moves to heading
- Color contrast: all text meets WCAG AA (4.5:1 for normal text, 3:1 for large text)
- Screen reader: recommendation cards announce rank, area, and score
- Avoid relying on color alone for status (use icons + text + color)
- `role="alert"` on error banners so screen readers announce them

---

## Environment Configuration

```typescript
// environments/environment.ts
export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5000'
};

// environments/environment.production.ts
export const environment = {
  production: true,
  apiBaseUrl: 'https://your-railway-app.up.railway.app'  // set at build time
};
```

Override at build time:
```bash
ng build --configuration production --define "environment.apiBaseUrl='https://api.prod.example.com'"
```

Or use Vercel environment variables + Angular build config.

---

## No SSR in V1

Angular app is built as a static SPA (`ng build` → `dist/`). Vercel serves it as static files with a `vercel.json` rewrite rule:

```json
{
  "rewrites": [{ "source": "/(.*)", "destination": "/index.html" }]
}
```

**SSR-compatibility rules (for future Phase 3 migration):**
- Never access `window`, `document`, `localStorage` directly in components
- Always use `SessionService` for storage access (it uses `isPlatformBrowser()` guard internally)
- No `window.open()` in components — wrap in a service method that checks platform
