using AI_Document_Intelligence.Models.DTOs;
using AI_Document_Intelligence.Services.Interfaces;
using AI_Document_Intelligence.Validation;
using Microsoft.AspNetCore.Mvc;

namespace AI_Document_Intelligence.Controllers;

[ApiController]
[Route("api/documents")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;

    public DocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DocumentListItemResponse>>> GetDocuments(
        [FromQuery] string? search,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<DocumentListItemResponse> documents =
            await _documentService.GetDocumentsAsync(
                search,
                status,
                cancellationToken);

        return Ok(documents);
    }

    [HttpPost("upload")]
    [RequestSizeLimit(DocumentFileValidator.MaxFileBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = DocumentFileValidator.MaxFileBytes)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<DocumentUploadResponse>> Upload(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        DocumentUploadResponse response =
            await _documentService.ProcessDocumentAsync(
                file!,
                cancellationToken);

        return Ok(response);
    }

    [HttpGet("{documentId:int:min(1)}")]
    public async Task<ActionResult<DocumentExtractionResponse>> GetDocument(
        int documentId,
        CancellationToken cancellationToken)
    {
        DocumentExtractionResponse? response =
            await _documentService.GetDocumentAsync(
                documentId,
                cancellationToken);

        if (response == null)
        {
            return NotFound(new
            {
                message = "Document not found."
            });
        }

        return Ok(response);
    }

    [HttpDelete("{documentId:int:min(1)}")]
    public async Task<IActionResult> DeleteDocument(
        int documentId,
        CancellationToken cancellationToken)
    {
        bool deleted = await _documentService.DeleteDocumentAsync(
            documentId,
            cancellationToken);

        if (!deleted)
        {
            return NotFound(new
            {
                message = "Document not found."
            });
        }

        return Ok(new
        {
            message = "Document deleted successfully."
        });
    }
}
