# Itinerary Create & Challenge Design

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a `/create` page where users who don't have an existing itinerary can either build one via a guided 4-step wizard (standard mode) or generate an off-the-beaten-path challenge itinerary with minimal input.

**Architecture:** Single new Angular route `/create` housing both modes on one page. Both modes call a new `POST /api/itinerary/generate` backend endpoint that returns a standard `ParsedItinerary`, which feeds directly into the existing `/review` → `/results` → `/hotels` pipeline. No new data models are required downstream.

**Tech Stack:** Angular (frontend), C# / .NET 8 (backend), Gemini AI (generation), existing `IAIProvider` abstraction, existing `ParsedItinerary` DTO.

---

## Data Flow

```
/create page
  ├── Standard mode: 4-step wizard → POST /api/itinerary/generate { mode: "standard", ... }
  └── Challenge mode: compact form → POST /api/itinerary/generate { mode: "challenge", ... }
        ↓ (both paths)
    Backend returns ParsedItinerary (same shape as /api/itinerary/parse)
        ↓
    store.setItinerary(result) → navigate to /review → /results → /hotels
```

---

## Request DTO

`ItineraryGenerationRequestDto` (C# record, snake_case on the wire):

| Field | Type | Notes |
|---|---|---|
| `mode` | `"standard"` \| `"challenge"` | Determines AI prompt |
| `duration_days` | `int` | e.g. 5 |
| `regions` | `string[]` | e.g. `["Kanto", "Kansai"]` |
| `travel_style` | `"cultural"` \| `"foodie"` \| `"nature"` \| `"urban"` \| `"mix"` | Standard mode only; ignored for challenge |
| `budget_tier` | `"budget"` \| `"mid"` \| `"luxury"` | Standard mode only; ignored for challenge |
| `pace` | `"relaxed"` \| `"moderate"` \| `"packed"` | Standard mode only; challenge always uses moderate-to-packed |

---

## Backend

### New Files

**`src/Application/DTOs/ItineraryGenerationRequestDto.cs`**
Record with the 6 fields above. Validation: `duration_days` must be 1–30; `regions` must have at least one entry; `mode` must be `standard` or `challenge`. When `mode` is `standard`, `travel_style`, `budget_tier`, and `pace` are required. When `mode` is `challenge`, these three fields are optional and ignored silently — no validation error if present.

**`src/Application/Services/Interfaces/IItineraryGenerationService.cs`**
```csharp
public interface IItineraryGenerationService
{
    Task<ParsedItinerary> GenerateItineraryAsync(ItineraryGenerationRequestDto request, CancellationToken ct = default);
}
```

**`src/Application/Services/ItineraryGenerationService.cs`**
Validates input, delegates to `IAIProvider.GenerateItineraryAsync`. No business logic beyond delegation and validation.

### Modified Files

**`src/Application/Interfaces/IAIProvider.cs`**
Add method:
```csharp
Task<ParsedItinerary> GenerateItineraryAsync(ItineraryGenerationRequestDto request, CancellationToken ct = default);
```

**`src/Infrastructure/Adapters/AI/GeminiAdapter.cs`**
Two prompt branches:

- **Standard prompt:** Instructs Gemini to build a `{duration_days}`-day itinerary for a `{travel_style}` traveller visiting `{regions}`, at `{pace}` pace, `{budget_tier}` budget. Returns the same `ParsedItinerary` JSON structure as the parse endpoint.
- **Challenge prompt:** Instructs Gemini to build a `{duration_days}`-day off-the-beaten-path itinerary for the specified regions. Explicitly forbids all major tourist hotspots (Senso-ji, Fushimi Inari, Shibuya Crossing, Dotonbori, etc.). Focuses on obscure rural towns, hidden temples, lesser-known neighborhoods, and destinations experienced Japan travellers seek out. Returns the same JSON structure.

Both branches reuse `ParseItineraryResponse` (existing helper) to deserialize the response.

**`src/Infrastructure/Adapters/AI/MockAIAdapter.cs`**
Stub returning a hardcoded 5-destination `ParsedItinerary` mixing Tokyo and Kyoto destinations with `parsingConfidence: "high"`.

**`src/Infrastructure/Adapters/AI/RulesOnlyAdapter.cs`**
Stub returning an empty `ParsedItinerary` with `clarificationNeeded: true` and `parsingConfidence: "low"`.

**`src/Infrastructure/Adapters/AI/CachedAIProvider.cs`**
- Standard mode: cache by SHA-256 hash of the full request (48h TTL) — same duration/regions/style/budget/pace always produces the same trip skeleton.
- Challenge mode: **no caching** — each challenge should feel fresh and unique.

**`src/API/Controllers/ItineraryController.cs`**
Add action:
```
POST /api/itinerary/generate
Body: ItineraryGenerationRequestDto
Returns: ParsedItineraryDto (200) | 400 validation error
```

**`src/API/Program.cs`**
Register `IItineraryGenerationService` → `ItineraryGenerationService` as scoped.

---

## Frontend

### New Files

**`frontend/src/app/features/itinerary/itinerary-create/itinerary-create.component.ts|html|scss`**

Single scrollable page at `/create` with two visually separated sections:

**Top section — "Build My Itinerary"**
- Step indicator (Step N of 4)
- Step 1 — Duration: button group "3–5 days" | "5–7 days" | "7–10 days" | "10+ days"
- Step 2 — Regions: multi-select chips (Kanto, Kansai, Chubu, Kyushu, Tohoku, Hokkaido, Okinawa)
- Step 3 — Travel style: icon cards (Cultural / Foodie / Nature / Urban / Mix)
- Step 4 — Pace + Budget: two button groups side by side
- "Generate My Itinerary →" button — disabled until all 4 steps are completed; shows spinner while loading

**Bottom section — "Itinerary Challenge"** (visually distinct dark/accent card)
- Headline: "Ready for a Challenge?"
- Description: "Skip the crowds. We'll build you a Japan trip most tourists never discover."
- Compact duration and regions inputs (no style/pace/budget needed for challenge)
- "Generate Challenge Itinerary ⚡" button — shows spinner while loading

**Shared component behaviour:**
- Both buttons call `api.generateItinerary(request)` 
- On success: `store.setItinerary(result)` + `router.navigate(['/review'])`
- On error: inline error banner with retry option
- Loading state disables both buttons simultaneously (prevent double-submit)

### Modified Files

**`frontend/src/app/core/models/itinerary.models.ts`**
Add:
```typescript
export type TravelStyle = 'cultural' | 'foodie' | 'nature' | 'urban' | 'mix';
export type Pace = 'relaxed' | 'moderate' | 'packed';
export type GenerationMode = 'standard' | 'challenge';

export interface ItineraryGenerationRequest {
  mode: GenerationMode;
  duration_days: number;
  regions: string[];
  travel_style?: TravelStyle;
  budget_tier?: BudgetTier;
  pace?: Pace;
}
```

**`frontend/src/app/core/services/api.service.ts`**
Add:
```typescript
generateItinerary(request: ItineraryGenerationRequest): Observable<ParsedItinerary> {
  return this.http.post<ParsedItinerary>(`${this.base}/api/itinerary/generate`, request)
    .pipe(timeout(60_000));
}
```

**`frontend/src/app/app.routes.ts`**
Add: `{ path: 'create', component: ItineraryCreateComponent }`

**`frontend/src/app/features/itinerary/itinerary-input/itinerary-input.component.html`**
Add below the paste textarea:
```html
<p class="create-link">
  Don't have an itinerary yet? 
  <a routerLink="/create">Build one →</a>
</p>
```

---

## Error Handling

- `duration_days` outside 1–30: 400 with `"Duration must be between 1 and 30 days."`
- `regions` empty: 400 with `"Please select at least one region."`
- `mode` invalid: 400 with `"Mode must be 'standard' or 'challenge'."`
- AI returns unparseable response: fall back to `FallbackItinerary` (existing helper) with `clarificationNeeded: true`; frontend shows error banner prompting user to retry

---

## Out of Scope

- Saving generated itineraries server-side (guest local storage handles this via existing session service)
- Challenge difficulty levels (one level only: off-the-beaten-path)
- User accounts or generation history
