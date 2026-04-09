namespace WhereToStayInJapan.Shared.Constants;

public static class RegionMappings
{
    public static readonly IReadOnlyDictionary<string, string> CityToRegion =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Tokyo"]     = "Kanto",
            ["Yokohama"]  = "Kanto",
            ["Kamakura"]  = "Kanto",
            ["Nikko"]     = "Kanto",
            ["Osaka"]     = "Kansai",
            ["Kyoto"]     = "Kansai",
            ["Nara"]      = "Kansai",
            ["Kobe"]      = "Kansai",
            ["Hiroshima"] = "Chugoku",
            ["Miyajima"]  = "Chugoku",
            ["Okayama"]   = "Chugoku",
        };
}
