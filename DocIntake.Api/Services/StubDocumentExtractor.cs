using System.Text.Json;

namespace DocIntake.Api.Services;

/// <summary>
/// Canned answers, no model call. Classification uses simple keywords; extraction returns a fixed,
/// internally consistent sample for the type. Keeps the pipeline and UI workable at zero cost,
/// and serves as the fallback when real extraction is switched off.
/// </summary>
public sealed class StubDocumentExtractor(ILogger<StubDocumentExtractor> log) : IDocumentExtractor
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public string Provider => "Stub";
    public string Model => "stub-canned";

    public Task<DocumentType> ClassifyAsync(string text, CancellationToken ct = default)
    {
        var t = text.ToLowerInvariant();
        var type =
            t.Contains("ach return") || t.Contains("returned ach") || t.Contains("return advice") ? DocumentType.AchReturnNotice :
            t.Contains("invoice") ? DocumentType.Invoice :
            t.Contains("opening balance") || t.Contains("beginning balance") || t.Contains("closing balance") || t.Contains("ending balance") ? DocumentType.BankStatement :
            t.Contains("amount due") || t.Contains("total due") || t.Contains("kwh") || t.Contains("therms") || t.Contains("ccf") || t.Contains("utility") ? DocumentType.UtilityBill :
            DocumentType.Unknown;

        log.LogInformation("Stub classified document as {DocumentType}", type);
        return Task.FromResult(type);
    }

    public Task<ExtractionResult> ExtractAsync(DocumentType documentType, string text, CancellationToken ct = default)
    {
        object sample = documentType switch
        {
            DocumentType.Invoice => new
            {
                vendorName = "Stub Supply Co.", invoiceNumber = "STUB-0001", invoiceDate = "2026-01-15", dueDate = "2026-02-14",
                currency = "USD", billTo = "Sample Customer LLC",
                lineItems = new[]
                {
                    new { description = "Sample item", quantity = 2m, unitPrice = 50.00m, amount = 100.00m },
                    new { description = "Second item", quantity = 1m, unitPrice = 25.00m, amount = 25.00m }
                },
                subtotal = 125.00m, tax = 10.94m, total = 135.94m
            },
            DocumentType.BankStatement => new
            {
                institutionName = "Stub Community Bank", accountHolder = "Sample Account Holder", accountType = "Checking", accountNumberMasked = "1234",
                periodStart = "2026-01-01", periodEnd = "2026-01-31", currency = "USD", openingBalance = 1000.00m, closingBalance = 1380.00m,
                transactions = new[]
                {
                    new { date = "2026-01-05", description = "Deposit", amount = 500.00m },
                    new { date = "2026-01-10", description = "Purchase", amount = -120.00m }
                }
            },
            DocumentType.AchReturnNotice => new
            {
                originatorName = "Stub Originator Inc.", receiverName = "Sample Receiver", rdfiName = "Stub Community Bank", secCode = "PPD",
                originalEffectiveDate = "2026-01-10", returnDate = "2026-01-13", traceNumber = "123456789012345", originalTraceNumber = "543210987654321",
                amount = 250.00m, returnReasonCode = "R01", returnReasonDescription = "Insufficient Funds"
            },
            DocumentType.UtilityBill => new
            {
                providerName = "Stub Electric Co-op", utilityType = "Electric", accountNumberMasked = "1234", serviceAddress = "1 Sample Street, Springfield, IL 62704",
                statementDate = "2026-02-05", periodStart = "2026-01-03", periodEnd = "2026-02-02", dueDate = "2026-02-26",
                usage = new { quantity = 640m, unit = "kWh" },
                charges = new[] { new { description = "Energy charge", amount = 80.00m }, new { description = "Customer charge", amount = 20.00m } },
                previousBalance = 90.00m, paymentsReceived = 90.00m, currentCharges = 100.00m, amountDue = 100.00m
            },
            _ => throw new InvalidOperationException($"Cannot extract a document of type {documentType}.")
        };

        var json = JsonSerializer.Serialize(sample, JsonOptions);
        log.LogInformation("Stub returned canned {DocumentType} data ({Characters} characters of input ignored)", documentType, text.Length);
        return Task.FromResult(new ExtractionResult(json, "stub.v1", InputTokens: 0, OutputTokens: 0));
    }
}
