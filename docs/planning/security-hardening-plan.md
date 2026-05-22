# Security Hardening Plan — Where To Stay In Japan

Status: Draft (2026-05-23)
Branch: `docs/security-hardening-plan`
Owner: TBD — implementation to be picked up by a follow-on agent.

This document (1) catalogs the security concepts that apply to a modern web application of this shape, (2) maps each concept to the current state of this codebase, (3) defines the prioritized set of gaps we will close, and (4) gives a concrete implementation guide that another agent can pick up and execute.

It also records the repo audit for sensitive files at the bottom.

---

## 1. Security Concepts Catalog (Reference)

Listed once here so the gap analysis below can reference these by name without re-explaining them.

### Network / Transport
- HTTPS / TLS termination
- HSTS (HTTP Strict Transport Security)
- Secure cookies (`Secure`, `HttpOnly`, `SameSite`)
- CORS allow-list (origins, headers, methods, credentials)

### Authentication & Authorization
- Authentication (session, JWT, OAuth, magic link)
- Authorization (role/claim/policy-based)
- Multi-factor authentication
- Session management (rotation, idle timeout, revocation)
- Principle of least privilege (DB roles, cloud IAM)

### Input & Output Safety
- Input validation (type, length, format, business rules)
- Output encoding / contextual escaping (XSS prevention)
- Parameterized queries / ORM (SQL injection prevention)
- File upload validation (extension, MIME, magic bytes, size, virus scan)
- Server-side validation always (never client-only)
- Path traversal protection
- SSRF prevention (outbound URL allow-listing)
- Deserialization safety

### Abuse / Resource Protection
- Rate limiting (per-IP, per-route, per-user, per-key)
- Request size limits
- Throttling / concurrency limits
- Quota enforcement for paid upstreams (Gemini, Rakuten)
- Bot/abuse detection, CAPTCHA on hot endpoints
- DoS protections (timeouts, circuit breakers)

### Secrets & Configuration
- Secrets management (env vars / secret store; never in repo)
- `.gitignore` discipline for env / settings files
- Pre-commit secret scanning
- Key rotation
- Separation of dev / staging / prod credentials
- Least-privileged API keys

### Browser Security Headers
- Content-Security-Policy (CSP)
- X-Content-Type-Options: nosniff
- X-Frame-Options / `frame-ancestors`
- Referrer-Policy
- Permissions-Policy
- Cross-Origin-Opener-Policy / Cross-Origin-Resource-Policy

### CSRF & Cross-Origin
- CSRF tokens (when using cookie auth)
- SameSite cookie defaults
- Origin / Referer validation on state-changing requests

### Data Protection
- Encryption at rest (DB-level)
- Encryption in transit (TLS everywhere, including DB connection)
- PII minimization
- Data retention / deletion policy
- Backup encryption and access control

### Logging, Monitoring, Observability
- Audit logging of security-relevant events
- Structured logs with correlation IDs
- Redaction of secrets/PII in logs
- Centralized error reporting
- Alerting on anomalies (rate-limit hits, 5xx spikes, auth failures)

### Operational / Supply Chain
- Dependency scanning (SCA — npm audit, dotnet list package --vulnerable)
- Static analysis (SAST)
- Container image scanning
- SBOM
- Minimum base images, non-root container user
- CI/CD secret hygiene, branch protection, required reviews
- Reproducible builds, signed images

### Application-Specific Hardening
- Idempotency keys on POST endpoints that mutate state
- Server-side authoritative limits (page size, request count)
- Resource ID opacity (UUID vs sequential)
- Defensive defaults (deny by default in auth/CORS/CSP)
- Graceful degradation when an upstream provider is unhealthy

### AI-Specific Threats (relevant here)
- Prompt injection from user-uploaded itinerary content
- Output validation (treat AI output as untrusted)
- Cost / token quota enforcement
- Logging of prompts/responses with PII consideration

---

## 2. Current State — What This Codebase Already Does Well

These were observed in the current code and should NOT be reworked:

- **CORS allow-list** is config-driven via `Cors:AllowedOrigins`; default is restrictive (no `AllowAnyOrigin`). See [Program.cs](../../src/API/Program.cs).
- **Global exception middleware** returns RFC 7807 ProblemDetails and only leaks `ex.Message` in Development. See [GlobalExceptionMiddleware.cs](../../src/API/Middleware/GlobalExceptionMiddleware.cs).
- **Request size limit** of 10 MB on itinerary parse endpoints. See [ItineraryController.cs](../../src/API/Controllers/ItineraryController.cs).
- **FluentValidation** wired up via `AddFluentValidationAutoValidation()`; validators exist for parse and preferences.
- **File extension allow-list** in [ParseItineraryRequestValidator.cs](../../src/Application/Validation/ParseItineraryRequestValidator.cs) (`.pdf`, `.docx`, `.txt`).
- **Parameterized DB access** through EF Core / Npgsql — no raw concatenated SQL observed.
- **Provider isolation** behind interfaces (`IAIProvider`, `IHotelProvider`, `IGeocodeProvider`, `IRoutingProvider`) — easier to add quotas/circuit-breakers.
- **Vercel proxy is gated** by `x-proxy-secret` header. See [rakuten.js](../../frontend/api/rakuten.js).
- **`.gitignore` is correct** for `.env`, `appsettings.Development.json`, `appsettings.Production.json`, `*.user`, `node_modules/`, logs. The three sensitive on-disk files (`.env`, `frontend/.env.playwright`, `src/API/appsettings.Development.json`) are confirmed untracked.
- **`appsettings.json` contains empty secret placeholders** only (no real keys).

---

## 3. Gap Analysis — What Is Missing

Severity legend:  **H** = high (do before public deploy), **M** = medium, **L** = low / nice-to-have.

| # | Gap | Severity | Where it shows up |
|---|-----|----------|-------------------|
| G1 | **No rate limiting wired up.** `RateLimit:*` exists in [appsettings.json](../../src/API/appsettings.json) but is unused. No `AddRateLimiter` in `Program.cs`. Parse/recommendation/chat/analytics endpoints are fully open. | H | [Program.cs](../../src/API/Program.cs) |
| G2 | **No security headers middleware.** No CSP, HSTS, `X-Content-Type-Options`, `Referrer-Policy`, `X-Frame-Options`/`frame-ancestors`, `Permissions-Policy`. | H | `Program.cs` |
| G3 | **`appsettings.json` allows `AllowedHosts: "*"`** in production config. Should be restricted to the Railway/Vercel hostnames in production. | M | [appsettings.json](../../src/API/appsettings.json) |
| G4 | **File upload validation is shallow.** `ItineraryController.ParseFileAsync` checks only that the file is non-empty — no extension allow-list at the controller boundary (the validator covers `ParseItineraryRequest`, not `IFormFile`), no MIME check, no magic-byte sniffing, no per-extractor size cap beyond the 10 MB `RequestSizeLimit`. | H | [ItineraryController.cs](../../src/API/Controllers/ItineraryController.cs) |
| G5 | **`ChatRequest.Message` and `ParseTextRequest.Text` are not length-bounded at the model level.** Parse text is bounded by the validator at 50,000 chars, but the chat endpoint is unbounded and feeds directly into Gemini → cost-amplification risk. | H | [ChatController.cs](../../src/API/Controllers/ChatController.cs) |
| G6 | **No prompt-injection defense.** User itinerary text and chat messages flow into `GeminiAdapter` prompts without sanitization, demarcation, or output validation. | M | `GeminiAdapter`, `ItineraryParsingService`, `ChatService` |
| G7 | **No CSRF protection.** Not blocking today because the API is bearer-less and CORS is locked, but if cookie-based auth is added in V1.1 (planner mentions magic-link), this becomes critical. Track now. | L (today) → H (when auth lands) | n/a |
| G8 | **No authentication / authorization scaffolding.** `app.UseAuthorization()` is called but no scheme is registered and no controller is `[Authorize]`. Acceptable for guest-only MVP but `AnalyticsController` and `HotelClickLog` accept attacker-controlled `SessionId` — abuse can poison analytics. | M | [AnalyticsController.cs](../../src/API/Controllers/AnalyticsController.cs) |
| G9 | **No SSRF guards on outbound HTTP.** OSRM/Nominatim base URLs are hardcoded (safe), but if a future feature accepts a URL from user input (e.g., "import itinerary from URL"), there is no helper to enforce allow-listing. Document the pattern now. | L | infra |
| G10 | **Connection string and API keys come from config, not from a typed secret-loading layer.** Acceptable on Railway (env vars), but the dev workflow relies on a real `.env` and an `appsettings.Development.json` that may have been hand-edited. Add a `.env.example` check in CI and a startup assertion that production keys are non-empty when `AI:Mode == production` etc. | M | `Program.cs` |
| G11 | **Logs are written to `src/API/logs/` and `*.log` is gitignored, but Serilog does not redact secrets/PII.** User itinerary content (potentially with email/phone) flows into logs at error time. | M | `Program.cs` Serilog config |
| G12 | **No dependency vulnerability scanning in CI.** No `dotnet list package --vulnerable` step; no `npm audit` gate. | M | CI |
| G13 | **Analytics endpoint is fire-and-forget with no validation.** `SessionId`/`HotelId` are free strings; attacker can flood `HotelClickLogs` table. Combine with G1 (rate limit) and add length caps + per-session quota. | M | `AnalyticsController` |
| G14 | **`Cors:AllowedOrigins` includes a preview Vercel deployment URL** (`...-r03l6apz7-...vercel.app`). Preview URLs rotate; leaving stale ones in the allow-list erodes the policy over time. | L | `appsettings.json` |
| G15 | **`.env.example` leaks real Supabase project ref + anon JWT.** See section 5 below — the URL `https://juksvitcuboskruayebk.supabase.co` and the anon JWT are checked into the repo. Anon keys are designed to be public *only when RLS is correctly configured*; publishing them in a public repo signals the project ref to any attacker and makes Row Level Security the only thing standing between users and the DB. | M (assuming RLS is on) / H (if RLS is off or weak) | [.env.example](../../.env.example) |
| G16 | **No Content-Security-Policy on the Angular app.** Vercel can set headers via `vercel.json`; current file does not configure them. | M | `frontend/vercel.json` |
| G17 | **HTTPS not enforced server-side.** No `app.UseHttpsRedirection()` and no HSTS. Railway terminates TLS at the edge, but defense-in-depth is still wanted. | M | `Program.cs` |

---

## 4. Implementation Guide (for the executing agent)

Work this plan **on its own feature branch**: `feature/security-hardening` (do NOT reuse `docs/security-hardening-plan`).

### Phase A — Quick wins, no design decisions needed (1 PR)

A1. **Add rate limiting** (closes G1). In `Program.cs`, after `AddControllers()`:

```csharp
using System.Threading.RateLimiting;

builder.Services.AddRateLimiter(opts =>
{
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    opts.AddPolicy("parse", ctx => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = builder.Configuration.GetValue<int>("RateLimit:ParseRequestsPerMinute", 10),
            Window = TimeSpan.FromMinutes(1)
        }));

    opts.AddPolicy("recommend", ctx => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = builder.Configuration.GetValue<int>("RateLimit:RecommendationRequestsPerMinute", 20),
            Window = TimeSpan.FromMinutes(1)
        }));

    opts.AddPolicy("chat", ctx => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        factory: _ => new FixedWindowRateLimiterOptions { PermitLimit = 15, Window = TimeSpan.FromMinutes(1) }));

    opts.AddPolicy("analytics", ctx => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        factory: _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1) }));
});
```

Then `app.UseRateLimiter()` after `UseCors()`, and decorate controllers:
- `ItineraryController` → `[EnableRateLimiting("parse")]`
- `RecommendationController` → `[EnableRateLimiting("recommend")]`
- `ChatController` → `[EnableRateLimiting("chat")]`
- `AnalyticsController` → `[EnableRateLimiting("analytics")]`

Add `RateLimit:ChatRequestsPerMinute` and `RateLimit:AnalyticsRequestsPerMinute` to `appsettings.json`.

A2. **Add security headers middleware** (closes G2, G17). Create `src/API/Middleware/SecurityHeadersMiddleware.cs`:

```csharp
public class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        var h = ctx.Response.Headers;
        h["X-Content-Type-Options"] = "nosniff";
        h["X-Frame-Options"] = "DENY";
        h["Referrer-Policy"] = "strict-origin-when-cross-origin";
        h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        if (ctx.Request.IsHttps)
            h["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        await next(ctx);
    }
}
```

Register before `UseCors()`. Also add `app.UseHttpsRedirection()` in production only.

A3. **Restrict `AllowedHosts`** (closes G3). Change `appsettings.json` value to a comma-separated list of allowed hostnames; override in Railway env (`AllowedHosts=wheretostayinjapan-production.up.railway.app`).

A4. **Bound `ChatRequest.Message`** (closes G5). Add a `ChatRequestValidator` (FluentValidation):

```csharp
public class ChatRequestValidator : AbstractValidator<ChatRequest>
{
    public ChatRequestValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.SessionId).MaximumLength(128);
    }
}
```

A5. **Tighten `AnalyticsController`** (closes G13). Validate lengths (`SessionId <= 128`, `HotelId <= 128`), reject if `SessionId` is empty, and stop using `Task.Run` for DB writes — use a bounded `Channel<HotelClickLog>` with a background consumer instead. Or simplest: just `await db.SaveChangesAsync()` and return `NoContent()` — the table is tiny.

A6. **Frontend security headers via `frontend/vercel.json`** (closes G16). Add a `headers` block with CSP (start in report-only), `X-Content-Type-Options`, `Referrer-Policy`, `Permissions-Policy`. Sketch:

```json
{
  "headers": [{
    "source": "/(.*)",
    "headers": [
      { "key": "X-Content-Type-Options", "value": "nosniff" },
      { "key": "Referrer-Policy", "value": "strict-origin-when-cross-origin" },
      { "key": "Permissions-Policy", "value": "camera=(), microphone=(), geolocation=()" },
      { "key": "Content-Security-Policy-Report-Only",
        "value": "default-src 'self'; script-src 'self'; connect-src 'self' https://wheretostayinjapan-production.up.railway.app; img-src 'self' data: https:; style-src 'self' 'unsafe-inline'; frame-ancestors 'none'" }
    ]
  }]
}
```

Promote to enforcing `Content-Security-Policy` after a week of clean reports.

### Phase B — File-upload hardening (1 PR)

B1. **Validate uploaded files at the controller** (closes G4). In `ItineraryController.ParseFileAsync`:

```csharp
private static readonly HashSet<string> AllowedFileExtensions = [".pdf", ".docx", ".txt"];
private static readonly Dictionary<string, byte[]> MagicBytes = new()
{
    [".pdf"]  = [0x25, 0x50, 0x44, 0x46],            // "%PDF"
    [".docx"] = [0x50, 0x4B, 0x03, 0x04],            // ZIP container
};

if (file.Length > 10 * 1024 * 1024) return BadRequest(...);
var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
if (!AllowedFileExtensions.Contains(ext)) return BadRequest(new { error = "Unsupported file type." });
// magic-byte check for pdf/docx; txt skipped
```

B2. **Treat `file.FileName` as untrusted.** Never write it to disk under its given name; if persisted, derive a server-side path from a GUID.

### Phase C — AI safety (1 PR)

C1. **Demarcate user content in prompts** (closes G6). In `GeminiAdapter` / `ChatService`, wrap user content with explicit delimiters and a system instruction telling the model the delimited content is data, not instructions. Don't rely on this alone — also bound output length and validate JSON shape on parse.

C2. **Token/cost budget per request.** `GeminiAdapter` already sets `maxOutputTokens` (recent commit `32c1627`). Add a per-IP daily token quota tracked in `PostgresCacheService` keyed by IP + UTC date.

C3. **Redact PII in logs** (closes G11). Add a Serilog enricher that masks email addresses and phone numbers in log messages. Skim user-content fields out of error logs.

### Phase D — Secrets & supply chain (1 PR)

D1. **Sanitize `.env.example`** (closes G15). Replace the real Supabase URL/anon JWT with placeholders:

```
SUPABASE_URL=https://your-project-ref.supabase.co
SUPABASE_ANON_KEY=your-anon-key
DB_HOST=db.your-project-ref.supabase.co
```

**Then rotate** the anon key and DB password in Supabase, since they were public. Confirm RLS is enabled on every table that contains user data; the anon key is only safe behind RLS. Do this even though the keys are "designed" to be public — public-on-GitHub broadcasts the project ref to attackers.

D2. **Startup assertion of required secrets.** In `Program.cs`, before `app.Run()`:

```csharp
if (aiMode == "production" && string.IsNullOrWhiteSpace(builder.Configuration["AI:GeminiApiKey"]))
    throw new InvalidOperationException("AI:GeminiApiKey missing in production mode.");
// repeat for hotelProvider == "rakuten", connection string, etc.
```

D3. **CI gates** (closes G12). Add a GitHub Actions workflow:
- `dotnet list package --vulnerable --include-transitive` (fail on >= moderate)
- `cd frontend && npm audit --audit-level=high`
- A secret-scan step (e.g., `gitleaks`).

D4. **Prune stale Vercel preview URL** from `Cors:AllowedOrigins` (closes G14). Replace with a regex policy or accept only `localhost:4200` + the stable production hostname.

### Phase E — Defer until auth lands (track only)

- G7 (CSRF) — add when magic-link auth is built. Plan: cookie auth with `SameSite=Lax`, plus double-submit CSRF tokens on state-changing routes; or stick to bearer tokens in `Authorization` headers and skip CSRF.

---

## 5. Repo Audit — Sensitive Files

Performed 2026-05-23 against branch `main` HEAD `32c1627`.

### Files present on disk
| Path | Tracked? | Status |
|---|---|---|
| `.env` | No (gitignored) | ✅ Safe |
| `frontend/.env.playwright` | No (gitignored) | ✅ Safe |
| `src/API/appsettings.Development.json` | No (gitignored) | ✅ Safe |
| `nul` (untracked, Windows reserved name) | No | Harmless artifact; recommend `Remove-Item .\nul -Force` |

### Tracked files scanned for secret patterns
- Searched 220 tracked files for: `eyJ…` JWTs, generic `key/secret/password/token = "<20+ chars>"`, Supabase project ref `juksvitcuboskruayebk`.
- **One hit: [`.env.example`](../../.env.example).**

### `.env.example` findings — action required
The file is committed to git and contains:
- A real Supabase **project URL**: `https://juksvitcuboskruayebk.supabase.co`
- A real Supabase **anon JWT** (`eyJ…`) for that project, valid until `exp: 2091-…`
- Real DB host `db.juksvitcuboskruayebk.supabase.co`

Why this matters even though the anon key is "public by design":
- The anon key only ties back to your project. Combined with the URL + DB host, an attacker has everything needed to enumerate your Supabase project and probe RLS policies.
- If RLS is **not** enabled on any table, this is a direct data-access vector.
- The Supabase docs treat anon keys as public, but Supabase's own guidance is *don't put them in a public repo's example file* — use placeholders, document the real values out-of-band.

**Required actions (do in this order):**
1. On a new branch `fix/sanitize-env-example`, replace the real values in `.env.example` with placeholders.
2. Commit and push (the JWT will still live in git history — see step 4).
3. In Supabase: confirm RLS is enabled on every table; review policies; **rotate** the anon key and DB password to invalidate the leaked ones.
4. Optional but recommended: scrub git history. Either `git filter-repo --replace-text` to redact the JWT from all prior commits, or accept the history and rely on the rotation. Either way, the leaked key must no longer be valid.
5. Add a `gitleaks` CI check (Phase D3 above) to catch the next one.

### Other observations
- `frontend/src/environments/environment.prod.ts` exposes the production Railway API URL. That's fine — it's a public endpoint by design, and security must not rely on its obscurity.
- `frontend/api/rakuten.js` reads `PROXY_SECRET`, `RAKUTEN_APP_ID`, `RAKUTEN_ACCESS_KEY` from Vercel env vars — no secrets in source. Good.
- `appsettings.json` ships with empty strings for `GeminiApiKey`, `ConnectionStrings:DefaultConnection`, `Hotels:ApiKey`. Good.

---

## 6. Suggested PR Sequence

1. `fix/sanitize-env-example` — replace real values, then rotate keys in Supabase. **Do first; small and urgent.**
2. `feature/security-hardening-phase-a` — rate limit + security headers + AllowedHosts + chat length cap + analytics tightening + vercel headers.
3. `feature/security-upload-validation` — magic-byte + extension check in controller.
4. `feature/ai-safety` — prompt demarcation, token quota, log redaction.
5. `feature/security-ci-gates` — dependency scan + gitleaks + startup assertions.

Each PR should keep changes scoped and reference the gap IDs (G1, G2, …) from this document in its description.
