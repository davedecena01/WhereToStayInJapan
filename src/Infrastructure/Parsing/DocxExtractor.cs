using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using WhereToStayInJapan.Application.Interfaces;

namespace WhereToStayInJapan.Infrastructure.Parsing;

public class DocxExtractor : IItineraryExtractor
{
    public bool CanHandle(string fileName) =>
        fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase);

    public Task<string> ExtractTextAsync(Stream stream, CancellationToken ct = default)
    {
        using var doc = WordprocessingDocument.Open(stream, isEditable: false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return Task.FromResult(string.Empty);

        var sb = new System.Text.StringBuilder();
        foreach (var para in body.Elements<Paragraph>())
        {
            sb.AppendLine(para.InnerText);
        }

        return Task.FromResult(sb.ToString());
    }
}
