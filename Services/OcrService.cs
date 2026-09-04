using AI_Document_Intelligence.Services.Interfaces;

namespace AI_Document_Intelligence.Services;

public class OcrService : IOcrService
{
    public Task<string> ExtractTextAsync(
        Stream documentStream,
        CancellationToken cancellationToken)
    {
        // Placeholder until an OCR provider (e.g. Azure Document Intelligence) is configured.
        return Task.FromResult(string.Empty);
    }
}
