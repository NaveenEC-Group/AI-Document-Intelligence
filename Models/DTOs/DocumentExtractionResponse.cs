namespace AI_Document_Intelligence.Models.DTOs;

public class DocumentExtractionResponse
{
    public int DocumentId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string DocumentType { get; set; } = string.Empty;

    public string ExtractedData { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }
}
