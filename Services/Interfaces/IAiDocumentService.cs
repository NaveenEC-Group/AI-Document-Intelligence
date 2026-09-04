namespace AI_Document_Intelligence.Services.Interfaces
{
    public interface IAiDocumentService
    {
        Task<string> ExtractStructuredDataAsync(
            string documentText,
            CancellationToken cancellationToken);
    }
}
