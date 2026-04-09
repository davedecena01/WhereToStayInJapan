namespace WhereToStayInJapan.Infrastructure.Adapters.Hotels;

public class MockHotelAdapter : IHotelProvider
{
    public Task<IReadOnlyList<HotelItem>> SearchAsync(HotelSearchParams searchParams, CancellationToken ct = default)
    {
        IReadOnlyList<HotelItem> results =
        [
            new("mock-hotel-001", "Sakura Business Hotel", null, 8500, 4.2, "https://travel.rakuten.co.jp/HOTEL/mock/1", 0.3),
            new("mock-hotel-002", "Grand Tokyo Inn", null, 12000, 4.5, "https://travel.rakuten.co.jp/HOTEL/mock/2", 0.5),
            new("mock-hotel-003", "Budget Stay Capsule", null, 4500, 3.8, "https://travel.rakuten.co.jp/HOTEL/mock/3", 0.8),
        ];
        return Task.FromResult(results);
    }
}
