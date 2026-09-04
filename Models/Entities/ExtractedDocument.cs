namespace AI_Document_Intelligence.Models.Entities
{
    public class ExtractedDocument
    {
        public int Id { get; set; }

        public int DocumentId { get; set; }

        public string DocumentType { get; set; } = string.Empty;

        public string ExtractedData { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public Document Document { get; set; } = null!;
    }
}
