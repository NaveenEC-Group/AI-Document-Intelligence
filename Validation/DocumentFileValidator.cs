using System.Text;

namespace AI_Document_Intelligence.Validation;

public static class DocumentFileValidator
{
    public const long MaxFileBytes = 10 * 1024 * 1024;
    public const int MaxFileNameLength = 255;

    private static readonly byte[] PdfMagic = Encoding.ASCII.GetBytes("%PDF");

    public static void Validate(IFormFile? file)
    {
        if (file is null)
        {
            throw new InvalidOperationException("A PDF file is required.");
        }

        if (string.IsNullOrWhiteSpace(file.FileName))
        {
            throw new InvalidOperationException("File name is required.");
        }

        string safeName = Path.GetFileName(file.FileName.Trim());

        if (string.IsNullOrWhiteSpace(safeName))
        {
            throw new InvalidOperationException("File name is invalid.");
        }

        if (safeName.Length > MaxFileNameLength)
        {
            throw new InvalidOperationException(
                $"File name must be {MaxFileNameLength} characters or fewer.");
        }

        if (safeName.Contains("..", StringComparison.Ordinal) ||
            safeName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new InvalidOperationException(
                "File name contains invalid characters.");
        }

        if (file.Length <= 0)
        {
            throw new InvalidOperationException("The uploaded file is empty.");
        }

        if (file.Length > MaxFileBytes)
        {
            throw new InvalidOperationException(
                "The maximum allowed file size is 10 MB.");
        }

        string extension = Path.GetExtension(safeName);

        if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only PDF files are supported.");
        }

        bool hasPdfContentType =
            string.IsNullOrWhiteSpace(file.ContentType) ||
            string.Equals(
                file.ContentType,
                "application/pdf",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                file.ContentType,
                "application/octet-stream",
                StringComparison.OrdinalIgnoreCase);

        if (!hasPdfContentType)
        {
            throw new InvalidOperationException(
                "Invalid content type. Upload a PDF document.");
        }

        ValidatePdfMagic(file);
    }

    public static string SanitizeFileName(string fileName)
    {
        string safeName = Path.GetFileName(fileName.Trim());

        if (string.IsNullOrWhiteSpace(safeName))
        {
            return "document.pdf";
        }

        if (safeName.Length > MaxFileNameLength)
        {
            safeName = safeName[..MaxFileNameLength];
        }

        return safeName;
    }

    private static void ValidatePdfMagic(IFormFile file)
    {
        byte[] header = new byte[PdfMagic.Length];

        using Stream stream = file.OpenReadStream();
        int read = stream.Read(header, 0, header.Length);

        if (read < PdfMagic.Length || !header.AsSpan().SequenceEqual(PdfMagic))
        {
            throw new InvalidOperationException(
                "File content is not a valid PDF.");
        }
    }
}
