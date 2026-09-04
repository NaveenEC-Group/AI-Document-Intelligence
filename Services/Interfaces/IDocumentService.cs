using AI_Document_Intelligence.Models.DTOs;

namespace AI_Document_Intelligence.Services.Interfaces;

public interface IDocumentService
{
    Task<DocumentUploadResponse> ProcessDocumentAsync(
        IFormFile file,
        CancellationToken cancellationToken);

    Task<DocumentExtractionResponse?> GetDocumentAsync(
        int documentId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DocumentListItemResponse>> GetDocumentsAsync(
        string? search,
        string? status,
        CancellationToken cancellationToken);

    Task<bool> DeleteDocumentAsync(
        int documentId,
        CancellationToken cancellationToken);
}
