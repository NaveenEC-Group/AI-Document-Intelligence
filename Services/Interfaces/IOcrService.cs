namespace AI_Document_Intelligence.Services.Interfaces
{
    public interface IOcrService
    {
        Task<string> ExtractTextAsync(
            Stream documentStream,
            CancellationToken cancellationToken);
    }
}
