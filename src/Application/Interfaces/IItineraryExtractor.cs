namespace WhereToStayInJapan.Application.Interfaces;

public interface IItineraryExtractor
{
    bool CanHandle(string fileName);
    Task<string> ExtractTextAsync(Stream stream, CancellationToken ct = default);
}
