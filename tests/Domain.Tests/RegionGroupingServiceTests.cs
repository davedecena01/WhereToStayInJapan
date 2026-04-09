using FluentAssertions;
using WhereToStayInJapan.Domain.Services;

namespace WhereToStayInJapan.Domain.Tests;

public class RegionGroupingServiceTests
{
    [Fact]
    public void HaversineDistance_TokyoToKyoto_Returns360kmApprox()
    {
        // Tokyo (~35.68, 139.69) to Kyoto (~35.01, 135.76)
        var km = RegionGroupingService.HaversineDistance(35.68, 139.69, 35.01, 135.76);

        km.Should().BeApproximately(360.0, 20.0);
    }

    [Fact]
    public void HaversineDistance_ShinjukuToShibuya_ReturnsUnder5km()
    {
        // Shinjuku (~35.69, 139.70) to Shibuya (~35.66, 139.70) — 3–4 km apart
        var km = RegionGroupingService.HaversineDistance(35.69, 139.70, 35.66, 139.70);

        km.Should().BeLessThan(5.0);
        km.Should().BeGreaterThan(1.0);
    }

    [Fact]
    public void HaversineDistance_SamePoint_ReturnsZero()
    {
        var km = RegionGroupingService.HaversineDistance(35.68, 139.69, 35.68, 139.69);

        km.Should().BeApproximately(0.0, 0.001);
    }
}
