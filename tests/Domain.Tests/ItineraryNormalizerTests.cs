using FluentAssertions;
using WhereToStayInJapan.Domain.Models;
using WhereToStayInJapan.Domain.Services;

namespace WhereToStayInJapan.Domain.Tests;

public class ItineraryNormalizerTests
{
    private static ItineraryNormalizer CreateNormalizer() =>
        new(new RegionGroupingService());

    private static ParsedItinerary Itinerary(params Destination[] destinations) => new()
    {
        Destinations = [.. destinations],
        ParsingConfidence = "high"
    };

    // --- Deduplication ---

    [Fact]
    public void Normalize_ExactDuplicateName_KeepsOnlyFirst()
    {
        var input = Itinerary(
            new Destination { Name = "Shinjuku Gyoen", City = "Tokyo" },
            new Destination { Name = "Shinjuku Gyoen", City = "Tokyo" }
        );

        var result = CreateNormalizer().Normalize(input);

        result.Destinations.Should().HaveCount(1);
        result.Destinations[0].Name.Should().Be("Shinjuku Gyoen");
    }

    [Fact]
    public void Normalize_CaseInsensitiveDuplicate_KeepsOnlyFirst()
    {
        var input = Itinerary(
            new Destination { Name = "Senso-ji Temple", City = "Tokyo" },
            new Destination { Name = "senso-ji temple", City = "Tokyo" }
        );

        var result = CreateNormalizer().Normalize(input);

        result.Destinations.Should().HaveCount(1);
    }

    [Fact]
    public void Normalize_SimilarNameDifferentCity_KeepsBoth()
    {
        // "Asakusa" (Tokyo) vs "Asakuza" (Kyoto) — Levenshtein 1, but different city → not a duplicate
        var input = Itinerary(
            new Destination { Name = "Asakusa", City = "Tokyo" },
            new Destination { Name = "Asakuza", City = "Kyoto" }
        );

        var result = CreateNormalizer().Normalize(input);

        result.Destinations.Should().HaveCount(2);
    }

    [Fact]
    public void Normalize_TypoWithinLevenshtein2_SameCity_Deduplicates()
    {
        // "Asakusa" vs "Asakuza" — 1-char diff, same city
        var input = Itinerary(
            new Destination { Name = "Asakusa", City = "Tokyo" },
            new Destination { Name = "Asakuza", City = "Tokyo" }
        );

        var result = CreateNormalizer().Normalize(input);

        result.Destinations.Should().HaveCount(1);
    }

    [Fact]
    public void Normalize_UniqueDestinations_KeepsAll()
    {
        var input = Itinerary(
            new Destination { Name = "Shinjuku", City = "Tokyo" },
            new Destination { Name = "Fushimi Inari", City = "Kyoto" },
            new Destination { Name = "Dotonbori", City = "Osaka" }
        );

        var result = CreateNormalizer().Normalize(input);

        result.Destinations.Should().HaveCount(3);
    }

    // --- Region lookup ---

    [Fact]
    public void Normalize_TokyoCity_AssignsKantoRegion()
    {
        var input = Itinerary(new Destination { Name = "Shibuya", City = "Tokyo" });

        var result = CreateNormalizer().Normalize(input);

        result.Destinations[0].Region.Should().Be("Kanto");
    }

    [Fact]
    public void Normalize_KyotoCity_AssignsKansaiRegion()
    {
        var input = Itinerary(new Destination { Name = "Arashiyama", City = "Kyoto" });

        var result = CreateNormalizer().Normalize(input);

        result.Destinations[0].Region.Should().Be("Kansai");
    }

    [Fact]
    public void Normalize_UnknownCity_LeavesRegionNull()
    {
        var input = Itinerary(new Destination { Name = "Some Place", City = "Hokkaido" });

        var result = CreateNormalizer().Normalize(input);

        result.Destinations[0].Region.Should().BeNull();
    }

    [Fact]
    public void Normalize_RegionAlreadySet_DoesNotOverwrite()
    {
        var input = Itinerary(new Destination { Name = "Place", City = "Tokyo", Region = "Custom" });

        var result = CreateNormalizer().Normalize(input);

        result.Destinations[0].Region.Should().Be("Custom");
    }

    // --- Day ordering ---

    [Fact]
    public void Normalize_OutOfOrderDays_SortsByDayNumber()
    {
        var input = Itinerary(
            new Destination { Name = "Hiroshima Peace Memorial", City = "Hiroshima", DayNumber = 3 },
            new Destination { Name = "Fushimi Inari", City = "Kyoto", DayNumber = 1 },
            new Destination { Name = "Dotonbori", City = "Osaka", DayNumber = 2 }
        );

        var result = CreateNormalizer().Normalize(input);

        result.Destinations.Select(d => d.DayNumber)
            .Should().ContainInOrder(1, 2, 3);
    }

    [Fact]
    public void Normalize_NullDayNumbers_SortedToEnd()
    {
        var input = Itinerary(
            new Destination { Name = "NullDay", DayNumber = null },
            new Destination { Name = "Day1Place", DayNumber = 1 }
        );

        var result = CreateNormalizer().Normalize(input);

        result.Destinations[0].DayNumber.Should().Be(1);
        result.Destinations[1].DayNumber.Should().BeNull();
    }

    [Fact]
    public void Normalize_NoDayNumbers_PreservesOriginalOrder()
    {
        var input = Itinerary(
            new Destination { Name = "First" },
            new Destination { Name = "Second" }
        );

        var result = CreateNormalizer().Normalize(input);

        result.Destinations[0].Name.Should().Be("First");
        result.Destinations[1].Name.Should().Be("Second");
    }

    // --- Multi-region detection ---

    [Fact]
    public void Normalize_TokyoAndKyoto_IsMultiRegionTrue()
    {
        var input = Itinerary(
            new Destination { Name = "Shinjuku", City = "Tokyo" },
            new Destination { Name = "Arashiyama", City = "Kyoto" }
        );

        var result = CreateNormalizer().Normalize(input);

        result.IsMultiRegion.Should().BeTrue();
        result.RegionsDetected.Should().Contain("Kanto").And.Contain("Kansai");
    }

    [Fact]
    public void Normalize_AllTokyoDestinations_IsMultiRegionFalse()
    {
        var input = Itinerary(
            new Destination { Name = "Shinjuku", City = "Tokyo" },
            new Destination { Name = "Asakusa", City = "Tokyo" },
            new Destination { Name = "Shibuya", City = "Tokyo" }
        );

        var result = CreateNormalizer().Normalize(input);

        result.IsMultiRegion.Should().BeFalse();
        result.RegionsDetected.Should().ContainSingle().Which.Should().Be("Kanto");
    }

    [Fact]
    public void Normalize_OsakaAndKyoto_SameKansaiRegion_IsMultiRegionFalse()
    {
        var input = Itinerary(
            new Destination { Name = "Dotonbori", City = "Osaka" },
            new Destination { Name = "Arashiyama", City = "Kyoto" }
        );

        var result = CreateNormalizer().Normalize(input);

        result.IsMultiRegion.Should().BeFalse();
        result.RegionsDetected.Should().ContainSingle().Which.Should().Be("Kansai");
    }

    // --- RegionsDetected output ---

    [Fact]
    public void Normalize_MultipleRegions_PopulatesRegionsDetected()
    {
        var input = Itinerary(
            new Destination { Name = "Shinjuku", City = "Tokyo" },
            new Destination { Name = "Itsukushima", City = "Miyajima" }
        );

        var result = CreateNormalizer().Normalize(input);

        result.RegionsDetected.Should().Contain("Kanto").And.Contain("Chugoku");
    }

    [Fact]
    public void Normalize_PreservesMetadata()
    {
        var input = new ParsedItinerary
        {
            Destinations = [],
            StartDate = new DateOnly(2025, 4, 1),
            EndDate = new DateOnly(2025, 4, 10),
            ParsingConfidence = "high",
            ClarificationNeeded = true,
            RawText = "some raw text"
        };

        var result = CreateNormalizer().Normalize(input);

        result.StartDate.Should().Be(new DateOnly(2025, 4, 1));
        result.EndDate.Should().Be(new DateOnly(2025, 4, 10));
        result.ParsingConfidence.Should().Be("high");
        result.ClarificationNeeded.Should().BeTrue();
        result.RawText.Should().Be("some raw text");
    }
}
