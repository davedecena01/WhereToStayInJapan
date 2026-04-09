# Deployment — Where To Stay In Japan

---

## Primary Deployment Stack

| Component | Service | Tier | Cost | Key Limitation |
|---|---|---|---|---|
| Frontend (Angular SPA) | **Vercel** | Free | $0 | None for static sites |
| Backend (.NET 8 API) | **Railway.app** | Starter (free credit) | ~$0–$5/mo | $5 free credit/month (~500 hrs) |
| Database (PostgreSQL) | **Supabase** | Free | $0 | 500MB storage, pauses after 1 week inactivity |

**Why Railway over Render.com for backend:**
- Railway does not sleep on idle (Render free tier sleeps after 15 min, causing 30s cold starts)
- Railway gives $5/month free credit — sufficient for ~500 hours, covering a portfolio demo
- Railway supports Docker deployments cleanly
- Render.com is a valid fallback if Railway credits run out

**Free-tier honest assessment:** These services are free for low-traffic portfolio use. None guarantee "free forever" under real load. Supabase pauses your database after 1 week of inactivity — a critical issue for an always-on demo. See workarounds below.

---

## Alternative Stack

| Component | Service | Notes |
|---|---|---|
| Frontend | **Netlify** | Equivalent to Vercel, slightly different DX |
| Backend | **Render.com** (free) | 30s cold start on idle — acceptable for demos where you warn visitors |
| Backend | **Fly.io** (free tier) | 3 shared VMs free; no sleep; requires Docker + `fly.toml` |
| Database | **Neon.tech** | PostgreSQL, free tier 0.5GB, does not pause on inactivity (better than Supabase for demos) |

**Recommended alternative if Supabase pause is a problem:** Swap to **Neon.tech** for the database — same PostgreSQL, Npgsql compatible, no inactivity pause on free tier.

---

## Dockerfile (Backend)

Location: `src/WhereToStayInJapan.API/Dockerfile`

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/WhereToStayInJapan.API/WhereToStayInJapan.API.csproj", "API/"]
COPY ["src/WhereToStayInJapan.Application/WhereToStayInJapan.Application.csproj", "Application/"]
COPY ["src/WhereToStayInJapan.Domain/WhereToStayInJapan.Domain.csproj", "Domain/"]
COPY ["src/WhereToStayInJapan.Infrastructure/WhereToStayInJapan.Infrastructure.csproj", "Infrastructure/"]
COPY ["src/WhereToStayInJapan.Shared/WhereToStayInJapan.Shared.csproj", "Shared/"]

RUN dotnet restore "API/WhereToStayInJapan.API.csproj"

COPY src/ .
WORKDIR /src/API

RUN dotnet publish "WhereToStayInJapan.API.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "WhereToStayInJapan.API.dll"]
```

Railway auto-detects the Dockerfile and builds on push. Set `PORT` to 8080 in Railway environment variables.

---

## Angular Build for Vercel

**`vercel.json`** (project root):
```json
{
  "buildCommand": "cd frontend && npm run build",
  "outputDirectory": "frontend/dist/where-to-stay-in-japan/browser",
  "rewrites": [
    { "source": "/(.*)", "destination": "/index.html" }
  ]
}
```

**Angular build:**
```bash
ng build --configuration production
```

**Note on `outputDirectory`:** Angular 17+ with SSR scaffold outputs to `dist/{project-name}/browser/`. Verify the exact path from `angular.json` → `projects.{name}.architect.build.options.outputPath`.

---

## Environment Variables

Set in Railway (backend) and Vercel (frontend) dashboards. Never commit secrets to git.

### Backend (Railway)

```
CONNECTIONSTRINGS__DEFAULTCONNECTION=postgresql://user:pass@host:5432/dbname?sslmode=require
AI__MODE=production
AI__PROVIDER=gemini
AI__APIKEY=your-gemini-api-key
AI__MODELID=gemini-1.5-flash
HOTELS__PROVIDER=rakuten
HOTELS__APIKEY=your-rakuten-app-id
HOTELS__SEARCHRADIUSKM=2
HOTELS__MINREVIEWRATING=3.5
MAPS__GEOCODEPROVIDER=nominatim
MAPS__ROUTINGPROVIDER=osrm
MAPS__USERAGENT=WhereToStayInJapan/1.0 (your-email@example.com)
CORS__ALLOWEDORIGINS__0=https://your-vercel-app.vercel.app
SENTRY__DSN=your-sentry-dsn (optional)
ASPNETCORE_ENVIRONMENT=Production
```

### Frontend (Vercel)

```
# Set as Vercel environment variable (Production only)
VITE_API_BASE_URL=https://your-railway-app.up.railway.app

# Or use Angular build-time replacement (preferred for Angular):
# Configured in vercel.json buildCommand:
# ng build --configuration production
# And replace in environment.production.ts at build time
```

Simplest approach: hardcode the Railway URL in `environment.production.ts` and commit it. The API URL is not a secret.

---

## Database Migration at Startup

EF Core migrations run automatically when the app starts:

```csharp
// Program.cs — after building the app
using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
await dbContext.Database.MigrateAsync();
// DataSeeder runs after migration via IHostedService
```

**Why this is safe for single-instance deployment:**
- Only one instance runs on Railway free tier
- EF Core migration is idempotent (tracks applied migrations in `__EFMigrationsHistory`)
- If migration fails, app crashes at startup → Railway shows error in logs
- Never run `MigrateAsync()` if you have multiple instances (race condition) — revisit in Phase 2

---

## CI/CD: GitHub Actions

**`.github/workflows/ci.yml`:**
```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  backend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - name: Restore
        run: dotnet restore WhereToStayInJapan.sln
      - name: Build
        run: dotnet build --no-restore -c Release
      - name: Test
        run: dotnet test --no-build -c Release --verbosity normal
        env:
          AI__MODE: mock
          HOTELS__PROVIDER: mock
          MAPS__GEOCODEPROVIDER: mock

  frontend:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: frontend
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: '20'
          cache: 'npm'
          cache-dependency-path: frontend/package-lock.json
      - run: npm ci
      - run: npm run build -- --configuration production
```

**Deployment:** Vercel and Railway both auto-deploy on push to `main` when connected to GitHub. No separate deploy step needed in CI.

---

## Supabase Inactivity Workaround

Supabase free tier pauses the database after 1 week of inactivity (no queries). This will break a portfolio demo that hasn't been visited recently.

**Workaround options:**

**Option A: Ping cron job (free)**
Use [cron-job.org](https://cron-job.org) (free) to ping `GET https://your-api.railway.app/api/health` every 6 days.
This triggers a DB query (health check queries DB status), resetting Supabase's inactivity timer.

**Option B: Upgrade Supabase**
Supabase Pro tier ($25/month) removes the inactivity pause. Only needed if you want a permanently-on demo.

**Option C: Switch to Neon.tech**
Neon free tier does not pause on inactivity. Same PostgreSQL compatibility. Simply update `CONNECTIONSTRINGS__DEFAULTCONNECTION` to the Neon connection string.

**Recommendation:** Start with Option A (cron-job.org ping). If the demo needs to be always-on professionally, switch to Neon.

---

## Railway Deployment Notes

1. Connect GitHub repo in Railway dashboard
2. Select the backend service → set `Root Directory` to project root (or use Dockerfile)
3. Add all environment variables listed above
4. Set health check path to `/api/health`
5. Railway auto-deploys on every push to `main`

**Railway $5 credit:** ~500 hours on a shared CPU instance. For a 30-day month, that's ~16 hours/day — effectively always-on for a demo. If credit runs out, Railway charges $0.000463/vCPU-hour — very low for minimal traffic.

---

## Vercel Deployment Notes

1. Import GitHub repo in Vercel dashboard
2. Set **Root Directory** to `frontend` (if Angular app is in a subfolder)
3. Set **Build Command**: `npm run build -- --configuration production`
4. Set **Output Directory**: `dist/where-to-stay-in-japan/browser` (check `angular.json`)
5. Add `vercel.json` at frontend root with the SPA rewrite rule
6. Add environment variables if needed (API base URL)
7. Vercel auto-deploys on push to `main`; creates preview deployments for PRs

---

## Local Development

```bash
# Backend (from repo root)
cd src/WhereToStayInJapan.API
dotnet run
# → http://localhost:5000

# Frontend (from repo root)
cd frontend
npm install
ng serve
# → http://localhost:4200

# Environment for local dev (no real API keys needed)
# appsettings.Development.json:
{
  "AI": { "Mode": "mock" },
  "Hotels": { "Provider": "mock" },
  "Maps": { "GeocodeProvider": "mock" }
}
```

**No real API keys required for local development.** All external calls use mock adapters.

---

## Connection String Formats

**Supabase (session mode, port 5432):**
```
Host=db.{project-ref}.supabase.co;Port=5432;Database=postgres;Username=postgres;Password={password};SSL Mode=Require;Trust Server Certificate=true
```

**Supabase (via PgBouncer/Supavisor, transaction mode, port 6543):**
```
Host=aws-0-{region}.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.{project-ref};Password={password};SSL Mode=Require;Trust Server Certificate=true
```

Use PgBouncer port (6543) in production to avoid connection exhaustion on free tier (max 60 concurrent connections on Supabase free).

**Neon (alternative):**
```
Host={project-id}.{region}.aws.neon.tech;Port=5432;Database=neondb;Username={user};Password={password};SSL Mode=Require;Trust Server Certificate=true
```
