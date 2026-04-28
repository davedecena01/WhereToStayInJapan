# Itinerary Create & Challenge Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a `/create` page with a 4-step wizard (standard mode) and an off-the-beaten-path challenge generator, both feeding into the existing `/review` → `/results` → `/hotels` pipeline.

**Architecture:** New `POST /api/itinerary/generate` endpoint backed by a new `IItineraryGenerationService` and a new `IAIProvider.GenerateItineraryAsync` method. Frontend adds a new Angular route `/create` with `ItineraryCreateComponent`. Both modes return a `ParsedItineraryDto` — the downstream pipeline is unchanged.

**Tech Stack:** C# / .NET 10, FluentValidation, Moq + FluentAssertions (tests), Angular 19+, TypeScript, SCSS.

---

## File Map

### Backend — New Files
- `src/Application/DTOs/ItineraryGenerationRequestDto.cs` — request record (6 fields)
- `src/Application/Validation/ItineraryGenerationRequestValidator.cs` — FluentValidation rules
- `src/Application/Services/Interfaces/IItineraryGenerationService.cs` — service interface
- `src/Application/Services/ItineraryGenerationService.cs` — delegates to IAIProvider, maps to DTO
- `tests/Application.Tests/ItineraryGenerationRequestValidatorTests.cs` — validator unit tests
- `tests/Application.Tests/ItineraryGenerationServiceTests.cs` — service unit tests

### Backend — Modified Files
- `src/Application/Interfaces/IAIProvider.cs` — add `GenerateItineraryAsync`
- `src/Infrastructure/Adapters/AI/MockAIAdapter.cs` — add stub returning hardcoded itinerary
- `src/Infrastructure/Adapters/AI/RulesOnlyAdapter.cs` — add stub returning empty itinerary
- `src/Infrastructure/Adapters/AI/GeminiAdapter.cs` — add standard + challenge prompt branches
- `src/Infrastructure/Adapters/AI/CachedAIProvider.cs` — cache standard, skip challenge
- `src/API/Controllers/ItineraryController.cs` — add `POST /api/itinerary/generate` action
- `src/API/Program.cs` — register `IItineraryGenerationService`

### Frontend — New Files
- `frontend/src/app/features/itinerary/itinerary-create/itinerary-create.component.ts`
- `frontend/src/app/features/itinerary/itinerary-create/itinerary-create.component.html`
- `frontend/src/app/features/itinerary/itinerary-create/itinerary-create.component.scss`

### Frontend — Modified Files
- `frontend/src/app/core/models/itinerary.models.ts` — add `TravelStyle`, `Pace`, `GenerationMode`, `ItineraryGenerationRequest`
- `frontend/src/app/core/services/api.service.ts` — add `generateItinerary` method
- `frontend/src/app/app.routes.ts` — add `/create` route
- `frontend/src/app/features/itinerary/itinerary-input/itinerary-input.component.html` — add "Build one →" link

---

## Task 1: Backend DTO + Validator

**Files:**
- Create: `src/Application/DTOs/ItineraryGenerationRequestDto.cs`
- Create: `src/Application/Validation/ItineraryGenerationRequestValidator.cs`
- Create: `tests/Application.Tests/ItineraryGenerationRequestValidatorTests.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/Application.Tests/ItineraryGenerationRequestValidatorTests.cs`:

```csharp
using FluentAssertions;
using WhereToStayInJapan.Application.DTOs;
using WhereToStayInJapan.Application.Validation;

namespace WhereToStayInJapan.Application.Tests;

public class ItineraryGenerationRequestValidatorTests
{
    private readonly ItineraryGenerationRequestValidator _validator = new();

    [Fact]
    public void Standard_valid_request_passes()
    {
        var req = new ItineraryGenerationRequestDto("standard", 5, ["Kanto"], "cultural", "mid", "moderate");
        _validator.Validate(req).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Challenge_valid_request_passes_without_style_pace_budget()
    {
        var req = new ItineraryGenerationRequestDto("challenge", 7, ["Kansai"], null, null, null);
        _validator.Validate(req).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_mode_fails_with_message()
    {
        var req = new ItineraryGenerationRequestDto("yolo", 5, ["Kanto"], "cultural", "mid", "moderate");
        var result = _validator.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Mode must be 'standard' or 'challenge'.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public void Duration_out_of_range_fails(int days)
    {
        var req = new ItineraryGenerationRequestDto("standard", days, ["Kanto"], "cultural", "mid", "moderate");
        var result = _validator.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Duration must be between 1 and 30 days.");
    }

    [Fact]
    public void Empty_regions_fails()
    {
        var req = new ItineraryGenerationRequestDto("standard", 5, [], "cultural", "mid", "moderate");
        var result = _validator.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Please select at least one region.");
    }

    [Fact]
    public void Standard_without_travel_style_fails()
    {
        var req = new ItineraryGenerationRequestDto("standard", 5, ["Kanto"], null, "mid", "moderate");
        _validator.Validate(req).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Standard_without_budget_fails()
    {
        var req = new ItineraryGenerationRequestDto("standard", 5, ["Kanto"], "cultural", null, "moderate");
        _validator.Validate(req).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Standard_without_pace_fails()
    {
        var req = new ItineraryGenerationRequestDto("standard", 5, ["Kanto"], "cultural", "mid", null);
        _validator.Validate(req).IsValid.Should().BeFalse();
    }
}
```

- [ ] **Step 2: Run tests — confirm they fail**

```powershell
cd "c:\Users\My PC\source\repos\WhereToStayInJapan"
dotnet test tests/Application.Tests --filter "ItineraryGenerationRequestValidatorTests" -v minimal 2>&1 | Select-String -Pattern "error|Error|FAILED|passed|failed"
```
Expected: build error — `ItineraryGenerationRequestDto` and `ItineraryGenerationRequestValidator` not found.

- [ ] **Step 3: Create the DTO**

Create `src/Application/DTOs/ItineraryGenerationRequestDto.cs`:

```csharp
namespace WhereToStayInJapan.Application.DTOs;

public record ItineraryGenerationRequestDto(
    string Mode,
    int DurationDays,
    List<string> Regions,
    string? TravelStyle,
    string? BudgetTier,
    string? Pace
);
```

- [ ] **Step 4: Create the validator**

Create `src/Application/Validation/ItineraryGenerationRequestValidator.cs`:

```csharp
using FluentValidation;
using WhereToStayInJapan.Application.DTOs;

namespace WhereToStayInJapan.Application.Validation;

public class ItineraryGenerationRequestValidator : AbstractValidator<ItineraryGenerationRequestDto>
{
    private static readonly HashSet<string> ValidModes = ["standard", "challenge"];
    private static readonly HashSet<string> ValidStyles = ["cultural", "foodie", "nature", "urban", "mix"];
    private static readonly HashSet<string> ValidBudgets = ["budget", "mid", "luxury"];
    private static readonly HashSet<string> ValidPaces = ["relaxed", "moderate", "packed"];

    public ItineraryGenerationRequestValidator()
    {
        RuleFor(x => x.Mode)
            .Must(m => ValidModes.Contains(m))
            .WithMessage("Mode must be 'standard' or 'challenge'.");

        RuleFor(x => x.DurationDays)
            .InclusiveBetween(1, 30)
            .WithMessage("Duration must be between 1 and 30 days.");

        RuleFor(x => x.Regions)
            .NotEmpty()
            .WithMessage("Please select at least one region.");

        When(x => x.Mode == "standard", () =>
        {
            RuleFor(x => x.TravelStyle)
                .NotEmpty()
                .Must(s => s == null || ValidStyles.Contains(s))
                .WithMessage("Travel style is required for standard mode.");

            RuleFor(x => x.BudgetTier)
                .NotEmpty()
                .Must(b => b == null || ValidBudgets.Contains(b))
                .WithMessage("Budget tier is required for standard mode.");

            RuleFor(x => x.Pace)
                .NotEmpty()
                .Must(p => p == null || ValidPaces.Contains(p))
                .WithMessage("Pace is required for standard mode.");
        });
    }
}
```

- [ ] **Step 5: Run tests — confirm they pass**

```powershell
dotnet test tests/Application.Tests --filter "ItineraryGenerationRequestValidatorTests" -v minimal
```
Expected: `8 passed, 0 failed`

- [ ] **Step 6: Commit**

```powershell
git checkout -b feature/itinerary-create-and-challenge
git add src/Application/DTOs/ItineraryGenerationRequestDto.cs
git add src/Application/Validation/ItineraryGenerationRequestValidator.cs
git add tests/Application.Tests/ItineraryGenerationRequestValidatorTests.cs
git commit -m "feat: add ItineraryGenerationRequestDto and validator"
```

---

## Task 2: IAIProvider Interface + Adapter Stubs

**Files:**
- Modify: `src/Application/Interfaces/IAIProvider.cs`
- Modify: `src/Infrastructure/Adapters/AI/MockAIAdapter.cs`
- Modify: `src/Infrastructure/Adapters/AI/RulesOnlyAdapter.cs`

- [ ] **Step 1: Add method to IAIProvider**

Edit `src/Application/Interfaces/IAIProvider.cs` — add the new method after `EditItineraryAsync`:

```csharp
using WhereToStayInJapan.Application.DTOs;
using WhereToStayInJapan.Domain.Models;

namespace WhereToStayInJapan.Application.Interfaces;

public interface IAIProvider
{
    Task<ParsedItinerary> ParseItineraryAsync(string rawText, CancellationToken ct = default);
    Task<ParsedItinerary> EditItineraryAsync(string instruction, ParsedItinerary current, CancellationToken ct = default);
    Task<ParsedItinerary> GenerateItineraryAsync(ItineraryGenerationRequestDto request, CancellationToken ct = default);
    Task<string> ChatAsync(string message, IEnumerable<string> destinations, CancellationToken ct = default);
    Task<string> GenerateExplanationAsync(string areaName, string city, IEnumerable<string> destinations, CancellationToken ct = default);
    Task<IReadOnlyList<string>> SuggestFoodAsync(string areaName, string city, int count, CancellationToken ct = default);
    Task<IReadOnlyList<string>> SuggestAttractionsAsync(string areaName, string city, int count, CancellationToken ct = default);
}
```

- [ ] **Step 2: Add stub to MockAIAdapter**

In `src/Infrastructure/Adapters/AI/MockAIAdapter.cs`, add after the `EditItineraryAsync` method:

```csharp
public Task<ParsedItinerary> GenerateItineraryAsync(ItineraryGenerationRequestDto request, CancellationToken ct = default)
{
    var result = new ParsedItinerary
    {
        ParsingConfidence = "high",
        ClarificationNeeded = false,
        Destinations =
        [
            new Destination { Name = "Shinjuku", City = "Tokyo", Region = "Kanto", DayNumber = 1 },
            new Destination { Name = "Asakusa", City = "Tokyo", Region = "Kanto", DayNumber = 2 },
            new Destination { Name = "Fushimi Inari", City = "Kyoto", Region = "Kansai", DayNumber = 3 },
            new Destination { Name = "Arashiyama", City = "Kyoto", Region = "Kansai", DayNumber = 4 },
            new Destination { Name = "Dotonbori", City = "Osaka", Region = "Kansai", DayNumber = 5 }
        ],
        RegionsDetected = ["Kanto", "Kansai"],
        IsMultiRegion = true
    };
    return Task.FromResult(result);
}
```

Also add the using at the top of MockAIAdapter.cs if not already present:
```csharp
using WhereToStayInJapan.Application.DTOs;
```

- [ ] **Step 3: Add stub to RulesOnlyAdapter**

In `src/Infrastructure/Adapters/AI/RulesOnlyAdapter.cs`, add after the `EditItineraryAsync` method:

```csharp
public Task<ParsedItinerary> GenerateItineraryAsync(ItineraryGenerationRequestDto request, CancellationToken ct = default)
    => Task.FromResult(new ParsedItinerary
    {
        ParsingConfidence = "low",
        ClarificationNeeded = true,
        Destinations = [],
        RegionsDetected = []
    });
```

Also add the using at the top of RulesOnlyAdapter.cs if not already present:
```csharp
using WhereToStayInJapan.Application.DTOs;
```

- [ ] **Step 4: Confirm the solution builds**

```powershell
dotnet build src/WhereToStayInJapan.sln -v minimal 2>&1 | Select-String -Pattern "error|warning|succeeded|failed"
```
Expected: `Build succeeded` — GeminiAdapter and CachedAIProvider will fail until Tasks 3–4.

- [ ] **Step 5: Commit**

```powershell
git add src/Application/Interfaces/IAIProvider.cs
git add src/Infrastructure/Adapters/AI/MockAIAdapter.cs
git add src/Infrastructure/Adapters/AI/RulesOnlyAdapter.cs
git commit -m "feat: add GenerateItineraryAsync to IAIProvider with mock and rules-only stubs"
```

---

## Task 3: GeminiAdapter — Standard & Challenge Prompts

**Files:**
- Modify: `src/Infrastructure/Adapters/AI/GeminiAdapter.cs`

- [ ] **Step 1: Add GenerateItineraryAsync to GeminiAdapter**

In `src/Infrastructure/Adapters/AI/GeminiAdapter.cs`, add the following after the `ChatAsync` method and before `GenerateExplanationAsync`. Also add `using WhereToStayInJapan.Application.DTOs;` at the top if not already present.

```csharp
public async Task<ParsedItinerary> GenerateItineraryAsync(ItineraryGenerationRequestDto request, CancellationToken ct = default)
{
    var regionList = string.Join(", ", request.Regions);
    var prompt = request.Mode == "challenge"
        ? BuildChallengePrompt(regionList, request.DurationDays)
        : BuildStandardPrompt(regionList, request.DurationDays, request.TravelStyle!, request.BudgetTier!, request.Pace!);

    var responseText = await CallGeminiAsync(prompt, ct);
    return ParseItineraryResponse(responseText, string.Empty);
}

private static string BuildStandardPrompt(string regions, int days, string style, string budget, string pace) => $$"""
    You are a Japan travel itinerary generator. Generate a {{days}}-day Japan itinerary for a {{style}} traveller visiting: {{regions}}, at {{pace}} pace, with a {{budget}} budget.

    Return ONLY a valid JSON object with this exact structure (no markdown, no explanation):
    {
      "destinations": [
        { "name": "place name", "city": "city name", "region": "Kanto|Kansai|Chubu|etc", "dayNumber": 1, "activityType": "sightseeing|food|accommodation|transport" }
      ],
      "regionsDetected": ["Kanto"],
      "isMultiRegion": false,
      "startDate": null,
      "endDate": null,
      "parsingConfidence": "high",
      "clarificationNeeded": false
    }

    Rules:
    - Generate destinations per day based on pace: relaxed=2 destinations/day, moderate=3/day, packed=4/day
    - Travel style guide: cultural=temples/shrines/museums, foodie=markets/restaurants/food streets, nature=parks/mountains/gardens, urban=shopping/nightlife/modern districts, mix=balanced combination
    - Set dayNumber correctly (1-based, matching the day of the trip)
    - Set parsingConfidence to "high" and clarificationNeeded to false
    - Return real, specific place names that exist in Japan
    - Use Japanese region names: Kanto (Tokyo area), Kansai (Kyoto/Osaka/Nara), Chubu (Nagoya/Fuji), Tohoku, Kyushu, Hokkaido, Okinawa
    """;

private static string BuildChallengePrompt(string regions, int days) => $$"""
    You are a Japan travel itinerary generator specializing in off-the-beaten-path experiences. Generate a {{days}}-day Japan challenge itinerary for: {{regions}}.

    STRICTLY FORBIDDEN — do not include these or anything similar:
    - Senso-ji Temple, Asakusa
    - Fushimi Inari Shrine
    - Shibuya Crossing / Scramble Square
    - Dotonbori, Namba
    - Arashiyama bamboo grove
    - Kinkaku-ji (Golden Pavilion)
    - teamLab venues, DisneySea, Universal Studios Japan
    - Tsukiji outer market
    - Shinjuku main tourist strips (Kabukicho, Omoide Yokocho)

    FOCUS ON:
    - Obscure rural towns (e.g. Tsumago, Magome, Ine, Naoshima, Tono, Kakunodate, Gujo Hachiman)
    - Hidden temples and shrines with no crowds
    - Local neighbourhood shotengai (covered shopping streets) not in tourist guides
    - Lesser-known onsen towns (not Hakone main area, not Beppu central)
    - Regional foods and markets tourists rarely visit
    - Places experienced Japan travellers specifically seek out

    Return ONLY a valid JSON object with this exact structure (no markdown, no explanation):
    {
      "destinations": [
        { "name": "place name", "city": "city name", "region": "Kanto|Kansai|Chubu|etc", "dayNumber": 1, "activityType": "sightseeing|food|accommodation|transport" }
      ],
      "regionsDetected": ["Kanto"],
      "isMultiRegion": false,
      "startDate": null,
      "endDate": null,
      "parsingConfidence": "high",
      "clarificationNeeded": false
    }

    Rules:
    - Generate 3–4 destinations per day
    - Every destination must be genuinely obscure — if a first-time Japan visitor would immediately recognise it, reject it
    - Set dayNumber correctly (1-based)
    - Set parsingConfidence to "high" and clarificationNeeded to false
    - Return real places that exist in Japan
    - Use Japanese region names: Kanto, Kansai, Chubu, Tohoku, Kyushu, Hokkaido, Okinawa
    """;
```

- [ ] **Step 2: Build to confirm GeminiAdapter compiles**

```powershell
dotnet build src/Infrastructure/WhereToStayInJapan.Infrastructure.csproj -v minimal 2>&1 | Select-String -Pattern "error|succeeded|failed"
```
Expected: `Build succeeded`

- [ ] **Step 3: Commit**

```powershell
git add src/Infrastructure/Adapters/AI/GeminiAdapter.cs
git commit -m "feat: implement GenerateItineraryAsync in GeminiAdapter with standard and challenge prompts"
```

---

## Task 4: CachedAIProvider — Cache Standard, Skip Challenge

**Files:**
- Modify: `src/Infrastructure/Adapters/AI/CachedAIProvider.cs`

- [ ] **Step 1: Add GenerateItineraryAsync to CachedAIProvider**

In `src/Infrastructure/Adapters/AI/CachedAIProvider.cs`, add after the `EditItineraryAsync` method:

```csharp
public async Task<ParsedItinerary> GenerateItineraryAsync(ItineraryGenerationRequestDto request, CancellationToken ct = default)
{
    // Challenge itineraries are never cached — each should feel unique
    if (request.Mode == "challenge")
        return await inner.GenerateItineraryAsync(request, ct);

    var input = $"{request.DurationDays}:{string.Join(",", request.Regions)}:{request.TravelStyle}:{request.BudgetTier}:{request.Pace}";
    var key = BuildHash("generate_itinerary", input.NormalizeKey());
    return await cache.GetOrSetAsync<ParsedItinerary>(
        key,
        async c => await inner.GenerateItineraryAsync(request, c),
        ExplainTtl,
        ct) ?? await inner.GenerateItineraryAsync(request, ct);
}
```

Also add the using at the top of CachedAIProvider.cs if not already present:
```csharp
using WhereToStayInJapan.Application.DTOs;
```

- [ ] **Step 2: Build the full solution**

```powershell
dotnet build src/WhereToStayInJapan.sln -v minimal 2>&1 | Select-String -Pattern "error|succeeded|failed"
```
Expected: `Build succeeded, 0 Error(s)`

- [ ] **Step 3: Commit**

```powershell
git add src/Infrastructure/Adapters/AI/CachedAIProvider.cs
git commit -m "feat: add GenerateItineraryAsync to CachedAIProvider (cache standard, skip challenge)"
```

---

## Task 5: IItineraryGenerationService + ItineraryGenerationService (TDD)

**Files:**
- Create: `src/Application/Services/Interfaces/IItineraryGenerationService.cs`
- Create: `src/Application/Services/ItineraryGenerationService.cs`
- Create: `tests/Application.Tests/ItineraryGenerationServiceTests.cs`

- [ ] **Step 1: Write failing service tests**

Create `tests/Application.Tests/ItineraryGenerationServiceTests.cs`:

```csharp
using FluentAssertions;
using Moq;
using WhereToStayInJapan.Application.DTOs;
using WhereToStayInJapan.Application.Interfaces;
using WhereToStayInJapan.Application.Services;
using WhereToStayInJapan.Domain.Models;

namespace WhereToStayInJapan.Application.Tests;

public class ItineraryGenerationServiceTests
{
    private readonly Mock<IAIProvider> _aiMock = new();
    private readonly ItineraryGenerationService _sut;

    public ItineraryGenerationServiceTests()
    {
        _sut = new ItineraryGenerationService(_aiMock.Object);
    }

    [Fact]
    public async Task GenerateItineraryAsync_standard_maps_ai_result_to_dto()
    {
        var request = new ItineraryGenerationRequestDto("standard", 3, ["Kanto"], "cultural", "mid", "moderate");
        var aiResult = new ParsedItinerary
        {
            Destinations = [new Destination { Name = "Ueno Park", City = "Tokyo", Region = "Kanto", DayNumber = 1 }],
            RegionsDetected = ["Kanto"],
            IsMultiRegion = false,
            ParsingConfidence = "high",
            ClarificationNeeded = false
        };
        _aiMock.Setup(a => a.GenerateItineraryAsync(request, default)).ReturnsAsync(aiResult);

        var result = await _sut.GenerateItineraryAsync(request);

        result.Destinations.Should().HaveCount(1);
        result.Destinations[0].Name.Should().Be("Ueno Park");
        result.Destinations[0].City.Should().Be("Tokyo");
        result.ParsingConfidence.Should().Be("high");
        result.ClarificationNeeded.Should().BeFalse();
        result.IsMultiRegion.Should().BeFalse();
        result.RegionsDetected.Should().ContainSingle("Kanto");
    }

    [Fact]
    public async Task GenerateItineraryAsync_challenge_delegates_to_ai_and_maps_result()
    {
        var request = new ItineraryGenerationRequestDto("challenge", 5, ["Kansai"], null, null, null);
        var aiResult = new ParsedItinerary
        {
            Destinations = [new Destination { Name = "Ine Funaya", City = "Ine", Region = "Kansai", DayNumber = 1 }],
            RegionsDetected = ["Kansai"],
            IsMultiRegion = false,
            ParsingConfidence = "high",
            ClarificationNeeded = false
        };
        _aiMock.Setup(a => a.GenerateItineraryAsync(request, default)).ReturnsAsync(aiResult);

        var result = await _sut.GenerateItineraryAsync(request);

        result.Destinations[0].Name.Should().Be("Ine Funaya");
        _aiMock.Verify(a => a.GenerateItineraryAsync(request, default), Times.Once);
    }

    [Fact]
    public async Task GenerateItineraryAsync_rawText_is_null_in_dto()
    {
        var request = new ItineraryGenerationRequestDto("standard", 3, ["Kanto"], "urban", "budget", "relaxed");
        _aiMock.Setup(a => a.GenerateItineraryAsync(request, default)).ReturnsAsync(new ParsedItinerary
        {
            Destinations = [],
            RegionsDetected = [],
            ParsingConfidence = "high",
            ClarificationNeeded = false
        });

        var result = await _sut.GenerateItineraryAsync(request);

        result.RawText.Should().BeNull();
    }
}
```

- [ ] **Step 2: Run tests — confirm they fail**

```powershell
dotnet test tests/Application.Tests --filter "ItineraryGenerationServiceTests" -v minimal 2>&1 | Select-String -Pattern "error|FAILED|passed|failed"
```
Expected: build error — `ItineraryGenerationService` not found.

- [ ] **Step 3: Create the service interface**

Create `src/Application/Services/Interfaces/IItineraryGenerationService.cs`:

```csharp
using WhereToStayInJapan.Application.DTOs;

namespace WhereToStayInJapan.Application.Services.Interfaces;

public interface IItineraryGenerationService
{
    Task<ParsedItineraryDto> GenerateItineraryAsync(ItineraryGenerationRequestDto request, CancellationToken ct = default);
}
```

- [ ] **Step 4: Create the service implementation**

Create `src/Application/Services/ItineraryGenerationService.cs`:

```csharp
using WhereToStayInJapan.Application.DTOs;
using WhereToStayInJapan.Application.Interfaces;
using WhereToStayInJapan.Application.Services.Interfaces;

namespace WhereToStayInJapan.Application.Services;

public class ItineraryGenerationService(IAIProvider ai) : IItineraryGenerationService
{
    public async Task<ParsedItineraryDto> GenerateItineraryAsync(
        ItineraryGenerationRequestDto request, CancellationToken ct = default)
    {
        var parsed = await ai.GenerateItineraryAsync(request, ct);

        return new ParsedItineraryDto(
            Destinations: parsed.Destinations.Select(d => new DestinationDto(
                d.Name, d.City, d.Region, d.DayNumber, d.ActivityType,
                d.Lat, d.Lng, d.IsAmbiguous)).ToList(),
            RegionsDetected: parsed.RegionsDetected,
            IsMultiRegion: parsed.IsMultiRegion,
            StartDate: parsed.StartDate,
            EndDate: parsed.EndDate,
            ParsingConfidence: parsed.ParsingConfidence,
            ClarificationNeeded: parsed.ClarificationNeeded,
            RawText: null);
    }
}
```

- [ ] **Step 5: Run tests — confirm they pass**

```powershell
dotnet test tests/Application.Tests --filter "ItineraryGenerationServiceTests" -v minimal
```
Expected: `3 passed, 0 failed`

- [ ] **Step 6: Run all application tests**

```powershell
dotnet test tests/Application.Tests -v minimal
```
Expected: all tests pass.

- [ ] **Step 7: Commit**

```powershell
git add src/Application/Services/Interfaces/IItineraryGenerationService.cs
git add src/Application/Services/ItineraryGenerationService.cs
git add tests/Application.Tests/ItineraryGenerationServiceTests.cs
git commit -m "feat: add ItineraryGenerationService with tests"
```

---

## Task 6: Controller Action + DI Registration

**Files:**
- Modify: `src/API/Controllers/ItineraryController.cs`
- Modify: `src/API/Program.cs`

- [ ] **Step 1: Add generate action to ItineraryController**

Replace the contents of `src/API/Controllers/ItineraryController.cs` with:

```csharp
using Microsoft.AspNetCore.Mvc;
using WhereToStayInJapan.Application.DTOs;
using WhereToStayInJapan.Application.Services.Interfaces;
using WhereToStayInJapan.Application.Validation;

namespace WhereToStayInJapan.API.Controllers;

[ApiController]
[Route("api/itinerary")]
public class ItineraryController(
    IItineraryParsingService parsingService,
    IItineraryGenerationService generationService) : ControllerBase
{
    [HttpPost("parse")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ParsedItineraryDto>> ParseAsync(
        [FromBody] ParseTextRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest(new { error = "Text is required." });

        var result = await parsingService.ParseTextAsync(request.Text, ct);
        return Ok(result);
    }

    [HttpPost("parse/file")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ParsedItineraryDto>> ParseFileAsync(
        IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "File is required." });

        await using var stream = file.OpenReadStream();
        var result = await parsingService.ParseFileAsync(stream, file.FileName, ct);
        return Ok(result);
    }

    [HttpPost("generate")]
    public async Task<ActionResult<ParsedItineraryDto>> GenerateAsync(
        [FromBody] ItineraryGenerationRequestDto request,
        CancellationToken ct)
    {
        var result = await generationService.GenerateItineraryAsync(request, ct);
        return Ok(result);
    }
}

public record ParseTextRequest(string Text);
```

- [ ] **Step 2: Register IItineraryGenerationService in Program.cs**

In `src/API/Program.cs`, find the line:
```csharp
builder.Services.AddScoped<IChatService, ChatService>();
```

Add immediately after it:
```csharp
builder.Services.AddScoped<IItineraryGenerationService, ItineraryGenerationService>();
```

- [ ] **Step 3: Build the full solution**

```powershell
dotnet build src/WhereToStayInJapan.sln -v minimal 2>&1 | Select-String -Pattern "error|succeeded|failed"
```
Expected: `Build succeeded, 0 Error(s)`

- [ ] **Step 4: Run all tests**

```powershell
dotnet test -v minimal 2>&1 | Select-String -Pattern "passed|failed|error"
```
Expected: all tests pass.

- [ ] **Step 5: Commit**

```powershell
git add src/API/Controllers/ItineraryController.cs
git add src/API/Program.cs
git commit -m "feat: add POST /api/itinerary/generate endpoint and DI registration"
```

---

## Task 7: Frontend Models + API Service

**Files:**
- Modify: `frontend/src/app/core/models/itinerary.models.ts`
- Modify: `frontend/src/app/core/services/api.service.ts`

- [ ] **Step 1: Add new types to itinerary.models.ts**

In `frontend/src/app/core/models/itinerary.models.ts`, add the following after the `AtmosphereType` type (around line 27):

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

- [ ] **Step 2: Add generateItinerary to api.service.ts**

In `frontend/src/app/core/services/api.service.ts`, add the following import at the top alongside the existing imports:
```typescript
import { ..., ItineraryGenerationRequest } from '../models/itinerary.models';
```

Then add the method after `parseFile`:

```typescript
generateItinerary(request: ItineraryGenerationRequest): Observable<ParsedItinerary> {
  return this.http.post<ParsedItinerary>(`${this.base}/api/itinerary/generate`, request)
    .pipe(timeout(60_000));
}
```

- [ ] **Step 3: Run Angular type check**

```powershell
cd frontend; npx tsc --noEmit 2>&1 | Select-String -Pattern "error"
```
Expected: no output (no errors).

- [ ] **Step 4: Commit**

```powershell
cd ..; git add frontend/src/app/core/models/itinerary.models.ts
git add frontend/src/app/core/services/api.service.ts
git commit -m "feat: add ItineraryGenerationRequest types and generateItinerary API method"
```

---

## Task 8: ItineraryCreateComponent

**Files:**
- Create: `frontend/src/app/features/itinerary/itinerary-create/itinerary-create.component.ts`
- Create: `frontend/src/app/features/itinerary/itinerary-create/itinerary-create.component.html`
- Create: `frontend/src/app/features/itinerary/itinerary-create/itinerary-create.component.scss`

- [ ] **Step 1: Create the component TypeScript**

Create `frontend/src/app/features/itinerary/itinerary-create/itinerary-create.component.ts`:

```typescript
import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { ItineraryStore } from '../../../core/stores/itinerary.store';
import {
  BudgetTier,
  GenerationMode,
  ItineraryGenerationRequest,
  Pace,
  TravelStyle
} from '../../../core/models/itinerary.models';

interface DurationOption { label: string; days: number; }
interface StyleOption { value: TravelStyle; label: string; icon: string; }
interface PaceOption { value: Pace; label: string; description: string; }

@Component({
  selector: 'app-itinerary-create',
  imports: [CommonModule, RouterModule],
  templateUrl: './itinerary-create.component.html',
  styleUrl: './itinerary-create.component.scss'
})
export class ItineraryCreateComponent {
  private readonly api = inject(ApiService);
  private readonly store = inject(ItineraryStore);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  // Standard wizard state
  readonly currentStep = signal(1);
  readonly selectedDuration = signal<number | null>(null);
  readonly selectedRegions = signal<string[]>([]);
  readonly selectedStyle = signal<TravelStyle | null>(null);
  readonly selectedPace = signal<Pace | null>(null);
  readonly selectedBudget = signal<BudgetTier | null>(null);

  // Challenge state
  readonly challengeDuration = signal<number | null>(null);
  readonly challengeRegions = signal<string[]>([]);

  readonly durationOptions: DurationOption[] = [
    { label: '3–5 days', days: 4 },
    { label: '5–7 days', days: 6 },
    { label: '7–10 days', days: 8 },
    { label: '10+ days', days: 12 }
  ];

  readonly regionOptions: string[] = ['Kanto', 'Kansai', 'Chubu', 'Kyushu', 'Tohoku', 'Hokkaido', 'Okinawa'];

  readonly styleOptions: StyleOption[] = [
    { value: 'cultural', label: 'Cultural', icon: '⛩️' },
    { value: 'foodie', label: 'Foodie', icon: '🍜' },
    { value: 'nature', label: 'Nature', icon: '🌿' },
    { value: 'urban', label: 'Urban', icon: '🏙️' },
    { value: 'mix', label: 'Mix', icon: '✨' }
  ];

  readonly paceOptions: PaceOption[] = [
    { value: 'relaxed', label: 'Relaxed', description: '2–3 spots/day' },
    { value: 'moderate', label: 'Moderate', description: '3–4 spots/day' },
    { value: 'packed', label: 'Packed', description: '4–5 spots/day' }
  ];

  readonly budgetOptions: { value: BudgetTier; label: string }[] = [
    { value: 'budget', label: 'Budget (¥5k–¥10k/night)' },
    { value: 'mid', label: 'Mid-range (¥10k–¥25k/night)' },
    { value: 'luxury', label: 'Luxury (¥25k+/night)' }
  ];

  readonly steps = [1, 2, 3, 4];

  get canGenerate(): boolean {
    return this.selectedDuration() !== null
      && this.selectedRegions().length > 0
      && this.selectedStyle() !== null
      && this.selectedPace() !== null
      && this.selectedBudget() !== null
      && !this.loading();
  }

  get canGenerateChallenge(): boolean {
    return this.challengeDuration() !== null
      && this.challengeRegions().length > 0
      && !this.loading();
  }

  toggleRegion(region: string): void {
    const current = this.selectedRegions();
    this.selectedRegions.set(
      current.includes(region) ? current.filter(r => r !== region) : [...current, region]
    );
  }

  toggleChallengeRegion(region: string): void {
    const current = this.challengeRegions();
    this.challengeRegions.set(
      current.includes(region) ? current.filter(r => r !== region) : [...current, region]
    );
  }

  goToStep(step: number): void { this.currentStep.set(step); }
  nextStep(): void { if (this.currentStep() < 4) this.currentStep.update(s => s + 1); }
  prevStep(): void { if (this.currentStep() > 1) this.currentStep.update(s => s - 1); }

  async generateStandard(): Promise<void> {
    if (!this.canGenerate) return;
    await this.generate({
      mode: 'standard',
      duration_days: this.selectedDuration()!,
      regions: this.selectedRegions(),
      travel_style: this.selectedStyle()!,
      budget_tier: this.selectedBudget()!,
      pace: this.selectedPace()!
    });
  }

  async generateChallenge(): Promise<void> {
    if (!this.canGenerateChallenge) return;
    await this.generate({
      mode: 'challenge',
      duration_days: this.challengeDuration()!,
      regions: this.challengeRegions()
    });
  }

  private async generate(request: ItineraryGenerationRequest): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const itinerary = await firstValueFrom(this.api.generateItinerary(request));
      this.store.setItinerary(itinerary);
      this.router.navigate(['/review']);
    } catch {
      this.error.set('Failed to generate itinerary. Please try again.');
    } finally {
      this.loading.set(false);
    }
  }
}
```

- [ ] **Step 2: Create the HTML template**

Create `frontend/src/app/features/itinerary/itinerary-create/itinerary-create.component.html`:

```html
<main class="create-page">
  <header class="page-header">
    <h1>Build Your Japan Itinerary</h1>
    <p class="subtitle">Answer a few questions and we'll design your perfect Japan trip.</p>
    <a routerLink="/" class="back-link">← Back to paste itinerary</a>
  </header>

  @if (error()) {
    <div class="error-banner" role="alert">
      <strong>Error:</strong> {{ error() }}
    </div>
  }

  <!-- Standard wizard -->
  <section class="wizard-section card">
    <h2>Build My Itinerary</h2>

    <div class="step-indicator" role="list" aria-label="Wizard progress">
      @for (step of steps; track step) {
        <button
          class="step-dot"
          role="listitem"
          [class.active]="currentStep() === step"
          [class.done]="currentStep() > step"
          (click)="goToStep(step)"
          [attr.aria-label]="'Go to step ' + step"
          [attr.aria-current]="currentStep() === step ? 'step' : null">
          {{ step }}
        </button>
        @if (step < 4) {
          <div class="step-line" [class.done]="currentStep() > step"></div>
        }
      }
    </div>

    <!-- Step 1: Duration -->
    @if (currentStep() === 1) {
      <div class="step-content">
        <h3>How long is your trip?</h3>
        <div class="option-group">
          @for (opt of durationOptions; track opt.days) {
            <button
              class="option-btn"
              [class.selected]="selectedDuration() === opt.days"
              (click)="selectedDuration.set(opt.days); nextStep()">
              {{ opt.label }}
            </button>
          }
        </div>
      </div>
    }

    <!-- Step 2: Regions -->
    @if (currentStep() === 2) {
      <div class="step-content">
        <h3>Which region(s) will you visit?</h3>
        <div class="chip-group">
          @for (region of regionOptions; track region) {
            <button
              class="chip"
              [class.selected]="selectedRegions().includes(region)"
              (click)="toggleRegion(region)">
              {{ region }}
            </button>
          }
        </div>
        <div class="step-nav">
          <button class="btn-secondary" (click)="prevStep()">← Back</button>
          <button
            class="btn-primary"
            [disabled]="selectedRegions().length === 0"
            (click)="nextStep()">
            Next →
          </button>
        </div>
      </div>
    }

    <!-- Step 3: Travel style -->
    @if (currentStep() === 3) {
      <div class="step-content">
        <h3>What's your travel style?</h3>
        <div class="style-grid">
          @for (opt of styleOptions; track opt.value) {
            <button
              class="style-card"
              [class.selected]="selectedStyle() === opt.value"
              (click)="selectedStyle.set(opt.value); nextStep()">
              <span class="style-icon" aria-hidden="true">{{ opt.icon }}</span>
              <span class="style-label">{{ opt.label }}</span>
            </button>
          }
        </div>
        <div class="step-nav">
          <button class="btn-secondary" (click)="prevStep()">← Back</button>
        </div>
      </div>
    }

    <!-- Step 4: Pace + Budget -->
    @if (currentStep() === 4) {
      <div class="step-content">
        <h3>Pace &amp; budget</h3>

        <div class="form-group">
          <p class="group-label">Daily pace</p>
          <div class="option-group">
            @for (opt of paceOptions; track opt.value) {
              <button
                class="option-btn option-btn--wide"
                [class.selected]="selectedPace() === opt.value"
                (click)="selectedPace.set(opt.value)">
                <strong>{{ opt.label }}</strong>
                <small>{{ opt.description }}</small>
              </button>
            }
          </div>
        </div>

        <div class="form-group">
          <p class="group-label">Budget per night</p>
          <div class="option-group">
            @for (opt of budgetOptions; track opt.value) {
              <button
                class="option-btn"
                [class.selected]="selectedBudget() === opt.value"
                (click)="selectedBudget.set(opt.value)">
                {{ opt.label }}
              </button>
            }
          </div>
        </div>

        <div class="step-nav">
          <button class="btn-secondary" (click)="prevStep()">← Back</button>
          <button
            class="btn-primary"
            [disabled]="!canGenerate"
            [attr.aria-busy]="loading()"
            (click)="generateStandard()">
            @if (loading()) {
              <span class="spinner" aria-hidden="true"></span>
              Generating&hellip;
            } @else {
              Generate My Itinerary →
            }
          </button>
        </div>
      </div>
    }
  </section>

  <!-- Challenge section -->
  <section class="challenge-section card card--dark">
    <div class="challenge-header">
      <span class="challenge-badge">⚡ Challenge Mode</span>
      <h2>Ready for a Challenge?</h2>
      <p class="challenge-description">Skip the crowds. We'll build you a Japan trip most tourists never discover.</p>
    </div>

    <div class="challenge-form">
      <div class="form-group">
        <p class="group-label">Trip length</p>
        <div class="option-group">
          @for (opt of durationOptions; track opt.days) {
            <button
              class="option-btn option-btn--ghost"
              [class.selected]="challengeDuration() === opt.days"
              (click)="challengeDuration.set(opt.days)">
              {{ opt.label }}
            </button>
          }
        </div>
      </div>

      <div class="form-group">
        <p class="group-label">Region(s)</p>
        <div class="chip-group">
          @for (region of regionOptions; track region) {
            <button
              class="chip chip--ghost"
              [class.selected]="challengeRegions().includes(region)"
              (click)="toggleChallengeRegion(region)">
              {{ region }}
            </button>
          }
        </div>
      </div>

      <button
        class="btn-challenge"
        [disabled]="!canGenerateChallenge"
        [attr.aria-busy]="loading()"
        (click)="generateChallenge()">
        @if (loading()) {
          <span class="spinner" aria-hidden="true"></span>
          Generating&hellip;
        } @else {
          Generate Challenge Itinerary ⚡
        }
      </button>
    </div>
  </section>
</main>
```

- [ ] **Step 3: Create the SCSS**

Create `frontend/src/app/features/itinerary/itinerary-create/itinerary-create.component.scss`:

```scss
.create-page {
  max-width: 780px;
  margin: 0 auto;
  padding: 2rem 1.5rem;
}

.page-header {
  text-align: center;
  margin-bottom: 2rem;

  h1 {
    font-size: 2rem;
    color: var(--color-navy);
    margin-bottom: 0.5rem;
  }

  .subtitle {
    color: #555;
    font-size: 1rem;
    margin-bottom: 0.75rem;
  }
}

.back-link {
  font-size: 0.85rem;
  color: var(--color-sakura);
  text-decoration: none;
  &:hover { text-decoration: underline; }
}

.card {
  background: #fff;
  border: 1px solid #e8e0d8;
  border-radius: 12px;
  padding: 1.5rem;
  margin-bottom: 1.5rem;

  h2 {
    font-size: 1.1rem;
    color: var(--color-navy);
    margin-bottom: 1.25rem;
    padding-bottom: 0.5rem;
    border-bottom: 1px solid #f0e8e0;
  }
}

.card--dark {
  background: var(--color-navy);
  border-color: var(--color-navy);
  color: #fff;

  h2 { color: #fff; border-bottom-color: rgba(255,255,255,0.15); }
}

// Step indicator
.step-indicator {
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 2rem;
}

.step-dot {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  border: 2px solid #ddd;
  background: #fff;
  color: #999;
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.15s;
  display: flex;
  align-items: center;
  justify-content: center;
  font-family: inherit;

  &.active {
    border-color: var(--color-sakura);
    background: var(--color-sakura);
    color: #fff;
  }

  &.done {
    border-color: var(--color-navy);
    background: var(--color-navy);
    color: #fff;
  }
}

.step-line {
  flex: 1;
  height: 2px;
  background: #ddd;
  max-width: 60px;
  transition: background 0.15s;
  &.done { background: var(--color-navy); }
}

.step-content {
  h3 {
    font-size: 1.05rem;
    color: var(--color-navy);
    margin-bottom: 1rem;
  }
}

// Option buttons
.option-group {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.option-btn {
  padding: 0.5rem 1rem;
  border: 2px solid #ddd;
  border-radius: 8px;
  background: #fff;
  font-size: 0.9rem;
  font-family: inherit;
  cursor: pointer;
  transition: all 0.15s;

  &:hover { border-color: var(--color-sakura); }

  &.selected {
    border-color: var(--color-navy);
    background: var(--color-navy);
    color: #fff;
  }

  &--wide {
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 0.15rem;
    padding: 0.6rem 1rem;
    small { font-size: 0.75rem; opacity: 0.75; }
  }

  &--ghost {
    border-color: rgba(255,255,255,0.3);
    background: transparent;
    color: #fff;
    &:hover { border-color: rgba(255,255,255,0.7); }
    &.selected { background: rgba(255,255,255,0.2); border-color: #fff; }
  }
}

// Region chips
.chip-group {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.chip {
  padding: 0.35rem 0.9rem;
  border: 1.5px solid #ddd;
  border-radius: 20px;
  background: #fff;
  font-size: 0.88rem;
  font-family: inherit;
  cursor: pointer;
  transition: all 0.15s;

  &:hover { border-color: var(--color-sakura); }
  &.selected { background: var(--color-sakura); border-color: var(--color-sakura); color: #fff; }

  &--ghost {
    border-color: rgba(255,255,255,0.3);
    background: transparent;
    color: #fff;
    &:hover { border-color: rgba(255,255,255,0.7); }
    &.selected { background: rgba(255,255,255,0.2); border-color: #fff; }
  }
}

// Style grid
.style-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(110px, 1fr));
  gap: 0.75rem;
  margin-bottom: 1rem;
}

.style-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.4rem;
  padding: 1rem 0.75rem;
  border: 2px solid #ddd;
  border-radius: 10px;
  background: #fff;
  font-family: inherit;
  cursor: pointer;
  transition: all 0.15s;

  .style-icon { font-size: 1.75rem; }
  .style-label { font-size: 0.85rem; font-weight: 600; color: var(--color-navy); }

  &:hover { border-color: var(--color-sakura); }
  &.selected { border-color: var(--color-navy); background: #f5f8ff; }
}

// Navigation
.step-nav {
  display: flex;
  justify-content: space-between;
  margin-top: 1.5rem;
}

.form-group { margin-bottom: 1.25rem; }

.group-label {
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--color-navy);
  margin-bottom: 0.5rem;
}

.card--dark .group-label { color: rgba(255,255,255,0.85); }

// Challenge section
.challenge-header {
  margin-bottom: 1.5rem;

  .challenge-badge {
    display: inline-block;
    background: rgba(255,255,255,0.15);
    border-radius: 12px;
    padding: 0.2rem 0.7rem;
    font-size: 0.75rem;
    font-weight: 700;
    letter-spacing: 0.05em;
    text-transform: uppercase;
    margin-bottom: 0.5rem;
  }

  .challenge-description { color: rgba(255,255,255,0.8); font-size: 0.95rem; }
}

// Buttons
.btn-primary {
  background: var(--color-navy);
  color: #fff;
  border: none;
  border-radius: 8px;
  padding: 0.65rem 1.5rem;
  font-size: 0.95rem;
  font-family: inherit;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  transition: background 0.15s;

  &:hover:not(:disabled) { background: #253d6a; }
  &:disabled { opacity: 0.5; cursor: not-allowed; }
}

.btn-secondary {
  background: transparent;
  color: var(--color-navy);
  border: 1.5px solid #ddd;
  border-radius: 8px;
  padding: 0.65rem 1.25rem;
  font-size: 0.95rem;
  font-family: inherit;
  cursor: pointer;
  transition: border-color 0.15s;
  &:hover { border-color: var(--color-navy); }
}

.btn-challenge {
  background: var(--color-sakura);
  color: #fff;
  border: none;
  border-radius: 8px;
  padding: 0.75rem 1.75rem;
  font-size: 1rem;
  font-family: inherit;
  font-weight: 600;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  transition: background 0.15s;
  margin-top: 0.5rem;

  &:hover:not(:disabled) { background: #e8829a; }
  &:disabled { opacity: 0.5; cursor: not-allowed; }
}

.error-banner {
  background: #fef2f2;
  border: 1px solid #fca5a5;
  color: #991b1b;
  padding: 0.75rem 1rem;
  border-radius: 8px;
  margin-bottom: 1rem;
  font-size: 0.9rem;
}

.spinner {
  width: 16px;
  height: 16px;
  border: 2px solid rgba(255,255,255,0.4);
  border-top-color: #fff;
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
  display: inline-block;
}

@keyframes spin { to { transform: rotate(360deg); } }

@media (max-width: 600px) {
  .create-page { padding: 1rem; }
  .page-header h1 { font-size: 1.5rem; }
  .style-grid { grid-template-columns: repeat(3, 1fr); }
  .step-nav { flex-direction: column-reverse; gap: 0.5rem; }
  .btn-primary, .btn-secondary { width: 100%; justify-content: center; }
}
```

- [ ] **Step 4: Confirm TypeScript compiles**

```powershell
cd frontend; npx tsc --noEmit 2>&1 | Select-String -Pattern "error"
```
Expected: no output.

- [ ] **Step 5: Commit**

```powershell
cd ..
git add frontend/src/app/features/itinerary/itinerary-create/
git commit -m "feat: add ItineraryCreateComponent with 4-step wizard and challenge mode"
```

---

## Task 9: Route + Home Page Link + Build Verification

**Files:**
- Modify: `frontend/src/app/app.routes.ts`
- Modify: `frontend/src/app/features/itinerary/itinerary-input/itinerary-input.component.html`

- [ ] **Step 1: Add /create route**

Replace the contents of `frontend/src/app/app.routes.ts` with:

```typescript
import { Routes } from '@angular/router';
import { ItineraryInputComponent } from './features/itinerary/itinerary-input/itinerary-input.component';
import { ItineraryReviewComponent } from './features/itinerary/itinerary-review/itinerary-review.component';
import { ItineraryCreateComponent } from './features/itinerary/itinerary-create/itinerary-create.component';
import { ResultsComponent } from './features/results/results/results.component';
import { HotelListComponent } from './features/hotels/hotel-list/hotel-list.component';

export const routes: Routes = [
  { path: '', component: ItineraryInputComponent },
  { path: 'create', component: ItineraryCreateComponent },
  { path: 'review', component: ItineraryReviewComponent },
  { path: 'results', component: ResultsComponent },
  { path: 'hotels/:areaId', component: HotelListComponent },
  { path: '**', redirectTo: '' }
];
```

- [ ] **Step 2: Add "Build one →" link to home page**

In `frontend/src/app/features/itinerary/itinerary-input/itinerary-input.component.html`, add `RouterModule` to the component imports in the `.ts` file. Then in the HTML, add the following after the closing `</section>` of the `text-section` card (around line 99, before the error banner):

```html
<p class="create-link">
  Don't have an itinerary yet?
  <a routerLink="/create">Build one →</a>
</p>
```

In `frontend/src/app/features/itinerary/itinerary-input/itinerary-input.component.ts`, add `RouterModule` to the imports array:

```typescript
imports: [CommonModule, FormsModule, RouterModule],
```

And add the import at the top:
```typescript
import { Router, RouterModule } from '@angular/router';
```

Then add the following to `itinerary-input.component.scss` (at the end):

```scss
.create-link {
  text-align: center;
  font-size: 0.85rem;
  color: #888;
  margin-top: -0.5rem;
  margin-bottom: 1rem;

  a {
    color: var(--color-sakura);
    text-decoration: none;
    font-weight: 500;
    &:hover { text-decoration: underline; }
  }
}
```

- [ ] **Step 3: Run Angular build**

```powershell
cd frontend; npx ng build --configuration production 2>&1 | Select-String -Pattern "error|Error|warning|complete"
```
Expected: `Application bundle generation complete.` with no errors.

- [ ] **Step 4: Run all backend tests one final time**

```powershell
cd ..; dotnet test -v minimal 2>&1 | Select-String -Pattern "passed|failed|error"
```
Expected: all tests pass.

- [ ] **Step 5: Final commit**

```powershell
git add frontend/src/app/app.routes.ts
git add frontend/src/app/features/itinerary/itinerary-input/itinerary-input.component.ts
git add frontend/src/app/features/itinerary/itinerary-input/itinerary-input.component.html
git add frontend/src/app/features/itinerary/itinerary-input/itinerary-input.component.scss
git commit -m "feat: add /create route and home page link to itinerary creator"
```

---

## Post-Implementation Checklist

After all tasks are complete:
- [ ] Push branch: `git push -u origin feature/itinerary-create-and-challenge`
- [ ] Merge to main and push to trigger Railway + Vercel deployment
- [ ] Wait 3–5 minutes for Railway backend deployment
- [ ] Run Playwright tests on the live app:
  - Navigate to `/create`
  - Complete the 4-step wizard (Kanto, 5–7 days, Cultural, Moderate, Mid-range) → verify lands on `/review` with destinations
  - Complete the challenge form (Kansai, 7–10 days) → verify lands on `/review` with off-the-beaten-path destinations
  - Verify "Build one →" link is visible on the home page
