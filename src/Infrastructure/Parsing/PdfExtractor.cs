using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using WhereToStayInJapan.Application.Interfaces;

namespace WhereToStayInJapan.Infrastructure.Parsing;

public class PdfExtractor : IItineraryExtractor
{
    public bool CanHandle(string fileName) =>
        fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

    public Task<string> ExtractTextAsync(Stream stream, CancellationToken ct = default)
    {
        using var pdf = PdfDocument.Open(stream);
        var sb = new System.Text.StringBuilder();

        foreach (Page page in pdf.GetPages())
        {
            sb.AppendLine(page.Text);
        }

        return Task.FromResult(sb.ToString());
    }
}
