namespace AI_Document_Intelligence.Models.Entities
{
    public class Document
    {
        public int Id { get; set; }

        public string FileName { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? ExtractedText { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public ICollection<ExtractedDocument> ExtractedDocuments { get; set; } =
            new List<ExtractedDocument>();
    }
}