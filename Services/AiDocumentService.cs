using System.ClientModel;
using System.Text.Json;
using AI_Document_Intelligence.Services.Interfaces;
using OpenAI.Chat;

namespace AI_Document_Intelligence.Services;

public class AiDocumentService : IAiDocumentService
{
    private readonly IConfiguration _configuration;

    public AiDocumentService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string> ExtractStructuredDataAsync(
        string documentText,
        CancellationToken cancellationToken)
    {
        string apiKey =
            _configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException(
                "OpenAI API key is not configured.");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "OpenAI API key is not configured.");
        }

        string model =
            _configuration["OpenAI:Model"] ?? "gpt-4o-mini";

        ChatClient client = new(model, apiKey);

        string prompt =
            """
            You are an AI document extraction system.

            Analyze the following document.

            Determine the document type.

            Always return ONLY valid JSON with this shape:
            {
              "documentType": "Invoice|Receipt|Contract|Other",
              "invoiceNumber": null,
              "invoiceDate": null,
              "vendor": null,
              "customer": null,
              "totalAmount": null,
              "currency": null,
              "lineItems": []
            }

            If the document is an invoice, fill invoice fields.
            Do not return markdown.
            Do not return explanations.

            Document:

            """
            + documentText;

        ClientResult<ChatCompletion> result =
            await client.CompleteChatAsync(
                [new UserChatMessage(prompt)],
                cancellationToken: cancellationToken);

        string outputText = result.Value.Content[0].Text;

        return NormalizeJson(outputText);
    }

    private static string NormalizeJson(string outputText)
    {
        string trimmed = outputText.Trim();

        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            int firstNewline = trimmed.IndexOf('\n');
            int lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);

            if (firstNewline >= 0 && lastFence > firstNewline)
            {
                trimmed = trimmed
                    .Substring(firstNewline + 1, lastFence - firstNewline - 1)
                    .Trim();
            }
        }

        using JsonDocument document = JsonDocument.Parse(trimmed);

        return document.RootElement.GetRawText();
    }
}
