# CLAUDE.md

## Project Overview

This repository is a portfolio-quality full-stack application called **Where To Stay in Japan**.

The application helps tourists determine the best area(s) to stay in Japan based on their itinerary, travel dates, preferences, and lodging priorities.

The app should:
- accept itinerary input from pasted text or uploaded files
- parse and normalize itinerary details
- recommend the best district/area + nearest major station to stay
- explain why each recommendation was chosen
- return at least 3 candidate stay areas where practical
- suggest nearby attractions related to the itinerary
- suggest food recommendations with exact location/address when possible
- fetch hotel options for recommended areas using a hotel API
- deep-link users to external booking pages

This project is intentionally designed as a **realistic, portfolio-grade travel planning application** built with a spec-driven workflow and disciplined AI-assisted development.

Claude must treat this repository as a **real engineering project**, not a throwaway demo.

---

## Primary Goal

The goal of this project is to build a **clean, maintainable, realistic AI-assisted trip planning application** using a spec-driven workflow.

Claude must prioritize:
- correctness
- maintainability
- readability
- consistency
- realistic engineering decisions
- token efficiency
- free-tier-friendly architecture
- strong portfolio value

Claude must NOT prioritize:
- unnecessary complexity
- overengineering
- premature optimization
- verbose output
- adding out-of-scope features
- speculative architecture without practical value

---

## Product Summary

This application helps users answer:

**"Given my itinerary in Japan, where should I stay?"**

The system should use a hybrid approach:
- **AI** for parsing assistance, explanation generation, nearby suggestions, and content assistance
- **deterministic logic** for repeatable scoring, filtering, grouping, ranking, and fallback behavior

This project is not a full OTA platform in V1.

### V1 Core Behavior
- user uploads or pastes itinerary
- supported V1 inputs:
  - PDF
  - DOCX
  - TXT
  - pasted text
- system extracts and normalizes itinerary
- user reviews and confirms parsed itinerary
- AI chat assists refinement where needed
- system recommends best lodging areas
- system returns hotel options using provider API
- user clicks out to external booking site

---

## Primary Users

- tourists visiting Japan

---

## Coverage Scope

### V1 Coverage
- major cities in Japan only

If itinerary includes far-apart regions, the app should:
- recommend multiple stay bases by region where appropriate
- warn that one single base may be inefficient

---

## Tech Stack

Claude should use this as the default stack unless explicitly instructed otherwise.

### Frontend
- Angular
- TypeScript
- HTML
- CSS / SCSS

### Backend
- C#
- .NET 8 Web API

### Data
- Prefer PostgreSQL unless specs or implementation constraints justify MongoDB
- Claude may recommend MongoDB only if there is a strong practical reason

### Integrations
- Hotel API: Rakuten Travel API first
- Maps / geocoding / routing: free-first provider strategy
- AI: provider-agnostic abstraction

### Hosting Goal
- free-tier-friendly deployment suitable for personal MVP / portfolio use

---

## Architecture Rules

Claude must follow this architecture unless explicitly instructed otherwise.

### Architectural Style
Use a clean, practical layered architecture:

- Controllers
- Services
- Repositories or data access layer
- DTOs
- Entities / models
- Provider adapters
- Scoring / ranking modules
- Parsing / normalization modules
- Caching layer
- Configuration / options
- Seeded content support

### Backend Responsibilities
- Controllers must remain thin
- Business logic belongs in services
- Data access belongs in repositories or focused data access classes
- External API integrations must be isolated behind provider interfaces/adapters
- API contracts must use DTOs, not persistence models directly
- Ranking logic must be explicit and testable
- AI prompts and orchestration must be isolated from controllers

### Frontend Structure
Use a clean Angular feature-based structure where practical.

Frontend should contain:
- itinerary intake flow
- parse/review confirmation flow
- recommendation results view
- hotel results view
- food / nearby suggestions section
- shared API services
- local storage support for guest saved itineraries

Frontend code should prioritize:
- readability
- separation of UI and API logic
- clear component responsibility
- predictable state flow

---

## AI + Deterministic Logic Rules

This project must use a hybrid approach.

### Deterministic Logic Must Handle
- itinerary normalization where possible
- grouping places by city/region
- candidate base area evaluation
- scoring and ranking recommendations
- hotel filtering
- station-distance logic
- route/travel-time scoring
- fallback behavior when AI is unavailable
- validation and normalization of parsed inputs

### AI Must Handle
- parsing assistance for messy itineraries
- explanation generation
- nearby attraction suggestions
- food recommendation assistance
- conversational refinement of itinerary details

### AI Guardrails
Claude must not design the app so that core correctness depends entirely on AI.

Claude must:
- prefer deterministic logic for repeatable system decisions
- make scoring logic explicit
- separate AI-generated explanation from computed recommendation results
- design fallback behavior if AI is unavailable
- keep AI provider integration replaceable

### AI Provider Rules
Architecture must support:
- mock mode
- rules-only fallback mode
- hosted open-model adapter
- future premium provider adapter

Claude must not hard-design the project around a single mandatory paid AI API.

---

## Spec-Driven Development Rules

This project follows a **spec-driven workflow**.

Claude must always treat the `/docs` folder as the source of truth for product and feature behavior.

Before implementing or changing a feature, Claude must check and follow relevant specs.

### Relevant documentation files may include
- `docs/product/project-spec.md`
- `docs/product/user-flows.md`
- `docs/technical/technical-spec.md`
- `docs/technical/system-architecture.md`
- `docs/technical/data-model.md`
- `docs/technical/api-contracts.md`
- `docs/technical/backend.md`
- `docs/technical/ui.md`
- `docs/technical/ai-strategy.md`
- `docs/technical/maps-and-routing.md`
- `docs/technical/hotel-integration.md`
- `docs/technical/auth-and-storage.md`
- `docs/technical/deployment.md`
- `docs/technical/observability.md`
- `docs/planning/execution-plan.md`
- `docs/planning/phased-roadmap.md`
- `docs/planning/risks-and-open-questions.md`

### Claude must
- implement based on documented requirements
- check specs before making design decisions
- ask for clarification only when truly necessary
- suggest missing spec details when necessary
- protect scope boundaries
- keep implementation aligned with phased delivery plan

### Claude must NOT
- silently add features not defined in spec
- drift into full OTA functionality in V1
- invent undocumented business rules
- expand infra complexity without justification

---

## MVP Scope Rules

This project should remain a focused MVP unless explicitly expanded.

### In Scope for MVP
- itinerary input from paste or upload
- file parsing for PDF, DOCX, TXT
- parsed itinerary review/confirmation
- AI-assisted refinement
- destination normalization
- recommendation of best stay areas
- district/area + nearest major station recommendations
- at least 3 candidate stay areas where practical
- explanation of why each area was chosen
- travel-time-aware ranking
- hotel lookup by recommended area
- hotel filtering by:
  - distance to station
  - budget
  - review/rating
- deep-link to external hotel booking
- nearby destination suggestions
- food suggestions
- guest mode
- local browser save for itineraries
- minimal logs and observability
- seeded curated content
- responsive desktop-first UI

### Out of Scope for MVP unless explicitly requested
- full in-app hotel booking flow
- payment processing
- enterprise admin system
- advanced auth/role management
- complex CMS
- real-time collaboration
- advanced analytics stack
- microservices
- event-driven architecture
- heavy background job infrastructure
- offline-first sync
- native mobile app
- excessive personalization engine
- fully automated content ingestion pipelines

Claude must protect the MVP from unnecessary feature creep.

---

## Coding Principles

Claude must write code that is:
- clean
- readable
- maintainable
- portfolio-worthy
- practical
- testable
- easy to reason about

### Prefer
- explicit names
- simple abstractions
- small focused methods
- predictable structure
- low-complexity control flow
- composable modules
- practical interfaces only where useful

### Avoid
- clever but hard-to-read code
- deep abstraction stacks
- speculative patterns
- generic enterprise boilerplate
- unnecessary indirection
- premature optimization
- verbose code when simple code is sufficient

---

## Token Conservation Rules

Claude must conserve tokens aggressively.

### Response Style Rules
- be concise by default
- prefer bullets over long prose when possible
- avoid repeating the same context
- do not restate specs unnecessarily
- do not produce motivational or filler language
- do not explain obvious code line by line unless asked
- do not generate large blocks of text when a compact answer is sufficient
- summarize first, expand only when needed
- when reviewing, focus only on material issues
- when planning, keep plans short and actionable

### Implementation Rules
- avoid rewriting large files unless required
- change only what is needed
- preserve existing structure where reasonable
- avoid generating duplicate helpers/utilities
- avoid unnecessary scaffolding
- avoid creating placeholder files without purpose
- avoid excessive comments in code
- avoid excessive test boilerplate

### Clarification Rules
- ask questions only when the answer materially affects architecture, correctness, or scope
- if reasonable assumptions are possible, state the assumption briefly and proceed

### Documentation Rules
- keep docs high-signal and implementation-oriented
- avoid repeating the same content across multiple docs
- prefer cross-reference over duplication

---

## Backend Coding Rules

### API Design
- use RESTful conventions
- use meaningful route naming
- support clear query parameters
- use appropriate HTTP status codes
- keep contracts stable and explicit

### Service Design
- services should orchestrate logic cleanly
- ranking and scoring logic should be isolated and testable
- external providers must be abstracted behind adapters/interfaces
- parsing and normalization logic should be modular

### Validation
- validate all write operations and important query inputs
- validate uploaded content handling
- validate itinerary structure after parsing
- return clear validation messages
- do not trust client input

### Error Handling
- use consistent API error responses
- prefer centralized/global exception handling where practical
- avoid leaking internal exception details
- distinguish validation, provider, parsing, and unexpected errors

### Data Rules
- use caching where it meaningfully reduces external provider usage
- prefer explicit models over loose structures
- keep persisted schema practical for querying recommendations and saved itineraries
- optimize only when justified by actual need

### DTO Rules
Use DTOs for:
- parsed itinerary response
- itinerary confirmation/update request
- recommendation response
- hotel result response
- food suggestion response
- nearby destination response
- error response

Do not expose persistence models directly in API contracts.

---

## Frontend Coding Rules

### UI Expectations
Frontend should be:
- clean
- practical
- readable
- desktop-first
- responsive enough for smaller screens
- visually themed but not overly decorative

### UX Requirements
- clear itinerary input flow
- parse review / confirmation step
- loading states
- validation messages
- error handling
- recommendation clarity
- visible explanation for why recommendations were chosen
- hotel deep-link clarity
- local save support for guest users

### Frontend Principles
- keep API logic in services
- keep components focused
- avoid bloated component files
- avoid complex state management unless justified
- prefer maintainable Angular patterns over trendy complexity

### Styling
- use a Japan / sakura-inspired theme with restraint
- prioritize usability over flair
- keep visual polish professional and portfolio-appropriate

---

## Data Model Expectations

Core domain concepts may include:
- Itinerary
- ItineraryDay
- ItineraryPlace
- ParsedPlace
- RecommendationRequest
- StayAreaRecommendation
- Station
- HotelSearchCache
- FoodSuggestion
- NearbyDestination
- CuratedContent
- AffiliateLink
- User
- SavedItinerary
- AppLog / ErrorLog / RecommendationLog

Claude may suggest schema adjustments if justified, but must keep the model practical and aligned with actual MVP needs.

---

## Recommendation Engine Rules

This is a core portfolio feature and must be implemented clearly.

### Recommendation Priorities
The default scoring should prioritize:
1. shortest total travel time
2. lowest hotel cost
3. near major stations
4. food access
5. shopping access

### Recommendation Output
Each recommendation should include:
- district/area name
- nearest major station
- why chosen
- estimated travel time to each destination
- pros and cons
- sample hotels
- food suggestions nearby
- extra attractions nearby

### Recommendation Behavior
- if itinerary spans far-apart regions, recommend multiple stay bases by region
- warn when one-base lodging is inefficient
- keep scoring logic explicit
- keep explanation generation separate from computed ranking

Claude should prioritize clarity and explainability over opaque ranking logic.

---

## Integrations Rules

### Hotel API
- Rakuten Travel API is the default first provider
- integration must remain provider-agnostic
- booking in V1 is external deep-link only
- avoid locking architecture to one provider

### Maps / Geocoding / Routing
- prefer free-first provider strategy
- design for provider abstraction
- assume rate limits and shared-service constraints
- use caching aggressively
- support future provider swap if needed

### File Parsing
- support PDF, DOCX, TXT, pasted text
- parsing flow must include review/confirmation step
- parsing must not assume perfect AI extraction
- keep file-processing logic modular

---

## Auth and Storage Rules

### V1 Auth
- guest mode first
- users can use planner without login
- guest itineraries may be saved in browser local storage

### Future Auth
- email magic link first
- optional social login later

### Storage Principles
- persist only what is valuable
- use caching for external provider responses when practical
- keep analytics minimal in MVP

---

## Observability Rules

MVP observability should remain minimal and practical.

### Save only
- basic app logs
- error logs
- recommendation request logs
- optional click tracking for hotel link clicks

Claude must not introduce heavy analytics tooling unless explicitly requested.

---

## Testing Expectations

Testing is encouraged where practical.

### Preferred initial test coverage
- itinerary normalization behavior
- recommendation scoring behavior
- multi-region split logic
- validation behavior
- provider adapter behavior
- fallback behavior when external services fail
- service-level business rules

Tests should remain:
- readable
- relevant
- not excessive
- focused on important logic

Do not generate large amounts of low-value test boilerplate.

---

## Mandatory Branching and Commit Workflow

Claude must never commit or push directly to `main`.

Before any major code, documentation, or structural change, Claude must:

1. check the current git branch
2. if on `main`, create a new branch before proceeding
3. use one of these prefixes:
   - `feature/description`
   - `fix/description`
   - `refactor/description`
   - `docs/description`
4. confirm or state the branch name being used
5. only then proceed with implementation

If branch creation is not possible, Claude must stop and explain the issue before continuing.

Claude must treat working directly on `main` as a workflow violation, not a suggestion.

### Commit Style
Use clear, professional commit messages.

Examples:
- `docs: add product and technical planning specs`
- `feat: implement itinerary parsing and review flow`
- `feat: add stay area recommendation engine`
- `feat: integrate rakuten hotel search`
- `feat: add guest itinerary local save support`
- `fix: handle invalid parsed itinerary segments`
- `refactor: simplify recommendation scoring service`

Claude should suggest commits that reflect meaningful units of work.

## Workflow for Claude

Claude should assist using this workflow:

### Before implementation
1. review relevant specs
2. identify exact scope of change
3. make only necessary assumptions
4. suggest concise implementation plan when useful

### During implementation
1. implement one scoped change at a time
2. keep changes localized
3. avoid touching unrelated files
4. preserve architecture consistency
5. conserve tokens and code churn

### After implementation
1. review for correctness and maintainability
2. verify against specs
3. suggest only valuable cleanup
4. suggest doc updates only if needed

Claude should behave like a careful senior engineer, not an uncontrolled code generator.

---

## Claude Review Behavior

When asked to review code, Claude should evaluate for:
- correctness
- maintainability
- readability
- architecture consistency
- token-efficient implementation
- unnecessary complexity
- validation gaps
- ranking logic clarity
- provider abstraction quality
- edge cases
- portfolio quality

Claude should provide practical feedback, not academic or overly theoretical critique.

---

## Portfolio Quality Rules

This project is intended for GitHub portfolio use.

Claude should help keep the project portfolio-worthy by encouraging:
- clean structure
- clear naming
- readable code
- practical engineering decisions
- strong README documentation
- disciplined scope
- realistic product behavior
- defensible architecture choices

Claude should avoid making the project look artificially overengineered just to appear senior.

Real quality is preferred over flashy complexity.

---

## Documentation Expectations

Claude should help maintain useful documentation for:
- setup instructions
- project overview
- architecture summary
- API behavior
- feature scope
- deployment notes
- future enhancements

Docs should remain concise, high-signal, and aligned with implementation.

---

## Important Guardrails

Claude must not:
- rewrite large parts of the codebase without justification
- add enterprise patterns without real value
- create unnecessary abstraction layers
- silently change API contracts
- invent undocumented business rules
- rely fully on AI for core recommendation correctness
- introduce paid-service assumptions without clearly stating them
- bloat responses or code with unnecessary verbosity

Claude should optimize for:
- shipping a clean MVP
- learning value
- maintainable architecture
- strong portfolio presentation
- token efficiency
- free-tier realism

---

## Constraints & Policies

Claude must strictly follow these rules when working on this repository.

### Security
- never expose secrets in code
- always use environment variables or secure configuration
- never hardcode API keys or tokens
- validate and sanitize user input
- do not trust client input
- do not expose internal exception details in responses

### Backend Code Quality
- follow clean layered architecture
- controllers must remain thin
- business logic must stay in services
- provider integrations must remain isolated
- avoid unnecessary abstractions
- prefer readability over cleverness
- keep methods focused
- avoid deep nesting

### Frontend Code Quality
- avoid use of `any` unless justified
- use clear typing
- keep components focused
- keep API calls inside services
- handle loading and error states clearly
- implement form validation cleanly

### Dependencies
- minimize external dependencies
- do not introduce new libraries unless necessary
- prefer built-in Angular and .NET features first
- avoid heavy UI frameworks for MVP
- avoid adding libraries when a simple local implementation is sufficient

### MVP Discipline
- do not add features outside defined MVP scope
- do not introduce enterprise-level complexity unnecessarily
- focus on completing current scope cleanly before expanding

### Code Review Mindset
When modifying or generating code, Claude must:
- check for readability and maintainability
- avoid duplication
- ensure consistency with existing structure
- verify alignment with specs in `/docs`
- prefer simple solutions over complex ones

---

## Documentation Maintenance Rules

Claude should keep documentation reasonably up to date when major milestones are completed.

After significant changes, Claude should suggest updates to relevant files such as:
- `docs/product/project-spec.md`
- `docs/technical/system-architecture.md`
- `docs/technical/api-contracts.md`
- `docs/technical/deployment.md`
- `docs/planning/execution-plan.md`
- `docs/project-status.md`
- `docs/changelog.md`

Claude should not rewrite documentation unnecessarily, but should help keep key docs aligned with implementation.