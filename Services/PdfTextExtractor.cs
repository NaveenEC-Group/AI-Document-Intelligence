using UglyToad.PdfPig;
using AI_Document_Intelligence.Services.Interfaces;

namespace AI_Document_Intelligence.Services;

public class PdfTextExtractor : IPdfTextExtractor
{
    public Task<string> ExtractTextAsync(
        Stream pdfStream,
        CancellationToken cancellationToken)
    {
        using PdfDocument pdfDocument = PdfDocument.Open(pdfStream);

        List<string> pageTexts = new();

        foreach (UglyToad.PdfPig.Content.Page page in pdfDocument.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();

            pageTexts.Add(page.Text);
        }

        string extractedText = string.Join(
            Environment.NewLine,
            pageTexts);

        return Task.FromResult(extractedText);
    }
}
