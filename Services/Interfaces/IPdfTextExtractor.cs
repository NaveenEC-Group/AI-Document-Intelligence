namespace AI_Document_Intelligence.Services.Interfaces
{
    public interface IPdfTextExtractor
    {
        Task<string> ExtractTextAsync(
            Stream pdfStream,
            CancellationToken cancellationToken);
    }
}
