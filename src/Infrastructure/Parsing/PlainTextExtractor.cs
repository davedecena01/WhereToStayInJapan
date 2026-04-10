using WhereToStayInJapan.Application.Interfaces;

namespace WhereToStayInJapan.Infrastructure.Parsing;

public class PlainTextExtractor : IItineraryExtractor
{
    public bool CanHandle(string fileName) =>
        fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase);

    public async Task<string> ExtractTextAsync(Stream stream, CancellationToken ct = default)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        return await reader.ReadToEndAsync(ct);
    }
}
