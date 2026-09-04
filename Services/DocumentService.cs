using System.Text.Json;
using AI_Document_Intelligence.Data;
using AI_Document_Intelligence.Models.DTOs;
using AI_Document_Intelligence.Models.Entities;
using AI_Document_Intelligence.Services.Interfaces;
using AI_Document_Intelligence.Validation;
using Microsoft.EntityFrameworkCore;

namespace AI_Document_Intelligence.Services;

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly IOcrService _ocrService;
    private readonly IAiDocumentService _aiDocumentService;

    public DocumentService(
        ApplicationDbContext dbContext,
        IPdfTextExtractor pdfTextExtractor,
        IOcrService ocrService,
        IAiDocumentService aiDocumentService)
    {
        _dbContext = dbContext;
        _pdfTextExtractor = pdfTextExtractor;
        _ocrService = ocrService;
        _aiDocumentService = aiDocumentService;
    }

    public async Task<DocumentUploadResponse> ProcessDocumentAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        DocumentFileValidator.Validate(file);

        string safeFileName = DocumentFileValidator.SanitizeFileName(file.FileName);

        Document document = new()
        {
            FileName = safeFileName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/pdf"
                : file.ContentType,
            FileSize = file.Length,
            Status = "Processing",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Documents.Add(document);

        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            string extractedText;

            await using Stream pdfStream = file.OpenReadStream();

            extractedText =
                await _pdfTextExtractor.ExtractTextAsync(
                    pdfStream,
                    cancellationToken);

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                await using Stream ocrStream = file.OpenReadStream();

                extractedText =
                    await _ocrService.ExtractTextAsync(
                        ocrStream,
                        cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                document.Status = "Failed";

                await _dbContext.SaveChangesAsync(cancellationToken);

                throw new InvalidOperationException(
                    "Unable to extract text from the document.");
            }

            if (extractedText.Length > 200_000)
            {
                extractedText = extractedText[..200_000];
            }

            document.ExtractedText = extractedText;

            string aiResult =
                await _aiDocumentService.ExtractStructuredDataAsync(
                    extractedText,
                    cancellationToken);

            if (string.IsNullOrWhiteSpace(aiResult))
            {
                throw new InvalidOperationException(
                    "AI extraction returned an empty response.");
            }

            ExtractedDocument extractedDocument = new()
            {
                DocumentId = document.Id,
                DocumentType = ResolveDocumentType(aiResult),
                ExtractedData = aiResult,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.ExtractedDocuments.Add(extractedDocument);

            document.Status = "Completed";
            document.ProcessedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new DocumentUploadResponse
            {
                DocumentId = document.Id,
                Message = "Document processed successfully."
            };
        }
        catch
        {
            document.Status = "Failed";

            await _dbContext.SaveChangesAsync(cancellationToken);

            throw;
        }
    }

    public async Task<DocumentExtractionResponse?> GetDocumentAsync(
        int documentId,
        CancellationToken cancellationToken)
    {
        if (documentId <= 0)
        {
            throw new InvalidOperationException("Document id must be a positive number.");
        }

        Document? document =
            await _dbContext.Documents
                .Include(item => item.ExtractedDocuments)
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.Id == documentId,
                    cancellationToken);

        if (document == null)
        {
            return null;
        }

        ExtractedDocument? extractedDocument =
            document.ExtractedDocuments
                .OrderByDescending(extracted => extracted.CreatedAt)
                .FirstOrDefault();

        return new DocumentExtractionResponse
        {
            DocumentId = document.Id,
            FileName = document.FileName,
            DocumentType =
                extractedDocument?.DocumentType ?? string.Empty,
            ExtractedData =
                extractedDocument?.ExtractedData ?? string.Empty,
            Status = document.Status,
            FileSize = document.FileSize,
            CreatedAt = document.CreatedAt,
            ProcessedAt = document.ProcessedAt
        };
    }

    public async Task<IReadOnlyList<DocumentListItemResponse>> GetDocumentsAsync(
        string? search,
        string? status,
        CancellationToken cancellationToken)
    {
        IQueryable<Document> query = _dbContext.Documents
            .AsNoTracking()
            .Include(item => item.ExtractedDocuments);

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim();
            query = query.Where(item => item.FileName.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            string statusFilter = status.Trim();
            query = query.Where(item => item.Status == statusFilter);
        }

        List<Document> documents = await query
            .OrderByDescending(item => item.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        return documents
            .Select(document =>
            {
                ExtractedDocument? extracted = document.ExtractedDocuments
                    .OrderByDescending(item => item.CreatedAt)
                    .FirstOrDefault();

                return new DocumentListItemResponse
                {
                    DocumentId = document.Id,
                    FileName = document.FileName,
                    Status = document.Status,
                    DocumentType = extracted?.DocumentType ?? string.Empty,
                    FileSize = document.FileSize,
                    CreatedAt = document.CreatedAt,
                    ProcessedAt = document.ProcessedAt
                };
            })
            .ToList();
    }

    public async Task<bool> DeleteDocumentAsync(
        int documentId,
        CancellationToken cancellationToken)
    {
        if (documentId <= 0)
        {
            throw new InvalidOperationException("Document id must be a positive number.");
        }

        Document? document = await _dbContext.Documents
            .FirstOrDefaultAsync(item => item.Id == documentId, cancellationToken);

        if (document == null)
        {
            return false;
        }

        _dbContext.Documents.Remove(document);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string ResolveDocumentType(string aiResult)
    {
        try
        {
            using JsonDocument json = JsonDocument.Parse(aiResult);

            if (json.RootElement.TryGetProperty(
                    "documentType",
                    out JsonElement documentTypeElement))
            {
                string? documentType = documentTypeElement.GetString();

                if (!string.IsNullOrWhiteSpace(documentType))
                {
                    return documentType.Trim();
                }
            }
        }
        catch (JsonException)
        {
            // Fall through to default.
        }

        return "Unknown";
    }
}
