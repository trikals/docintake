using System.Globalization;
using System.Text.Json;
using DocIntake.Api.Models;

namespace DocIntake.Api.Services;

/// <summary>
/// Maps a person-confirmed JSON object onto the relational entities.
/// Assumes DocumentValidator already accepted the JSON (required fields present, right types and formats),
/// so reads here use GetProperty directly.
/// </summary>
public static class ConfirmedDataMapper
{
    public static Invoice ToInvoice(int documentId, JsonElement j)
    {
        var invoice = new Invoice
        {
            DocumentId = documentId,
            VendorName = Str(j, "vendorName"),
            InvoiceNumber = Str(j, "invoiceNumber"),
            InvoiceDate = Date(j, "invoiceDate"),
            DueDate = OptDate(j, "dueDate"),
            Currency = Str(j, "currency"),
            BillTo = OptStr(j, "billTo"),
            Subtotal = Dec(j, "subtotal"),
            Tax = Dec(j, "tax"),
            Total = Dec(j, "total")
        };

        var n = 1;
        foreach (var line in j.GetProperty("lineItems").EnumerateArray())
        {
            invoice.Lines.Add(new InvoiceLine
            {
                LineNumber = n++,
                Description = Str(line, "description"),
                Quantity = Dec(line, "quantity"),
                UnitPrice = Dec(line, "unitPrice"),
                Amount = Dec(line, "amount")
            });
        }
        return invoice;
    }

    public static BankStatement ToBankStatement(int documentId, JsonElement j)
    {
        var statement = new BankStatement
        {
            DocumentId = documentId,
            InstitutionName = Str(j, "institutionName"),
            AccountHolder = Str(j, "accountHolder"),
            AccountType = Enum.Parse<AccountType>(Str(j, "accountType")),
            AccountNumberMasked = Str(j, "accountNumberMasked"),
            PeriodStart = Date(j, "periodStart"),
            PeriodEnd = Date(j, "periodEnd"),
            Currency = Str(j, "currency"),
            OpeningBalance = Dec(j, "openingBalance"),
            ClosingBalance = Dec(j, "closingBalance")
        };

        var n = 1;
        foreach (var t in j.GetProperty("transactions").EnumerateArray())
        {
            // The JSON carries a signed amount; the table stores a direction and a positive amount.
            var signed = Dec(t, "amount");
            statement.Transactions.Add(new BankTransaction
            {
                LineNumber = n++,
                TransactionDate = Date(t, "date"),
                Description = Str(t, "description"),
                Direction = signed < 0 ? TransactionDirection.Debit : TransactionDirection.Credit,
                Amount = Math.Abs(signed)
            });
        }
        return statement;
    }

    public static AchReturnNotice ToAchReturnNotice(int documentId, JsonElement j) => new()
    {
        DocumentId = documentId,
        OriginatorName = Str(j, "originatorName"),
        ReceiverName = Str(j, "receiverName"),
        RdfiName = Str(j, "rdfiName"),
        SecCode = Str(j, "secCode"),
        OriginalEffectiveDate = Date(j, "originalEffectiveDate"),
        ReturnDate = Date(j, "returnDate"),
        TraceNumber = Str(j, "traceNumber"),
        OriginalTraceNumber = Str(j, "originalTraceNumber"),
        Amount = Dec(j, "amount"),
        ReturnReasonCode = Str(j, "returnReasonCode").Trim().ToUpperInvariant(),
        ReturnReasonDescription = Str(j, "returnReasonDescription")
    };

    public static UtilityBill ToUtilityBill(int documentId, JsonElement j)
    {
        var usage = j.GetProperty("usage");
        var bill = new UtilityBill
        {
            DocumentId = documentId,
            ProviderName = Str(j, "providerName"),
            UtilityType = Enum.Parse<UtilityType>(Str(j, "utilityType")),
            AccountNumberMasked = Str(j, "accountNumberMasked"),
            ServiceAddress = Str(j, "serviceAddress"),
            StatementDate = Date(j, "statementDate"),
            PeriodStart = Date(j, "periodStart"),
            PeriodEnd = Date(j, "periodEnd"),
            DueDate = Date(j, "dueDate"),
            UsageQuantity = Dec(usage, "quantity"),
            UsageUnit = Str(usage, "unit"),
            PreviousBalance = Dec(j, "previousBalance"),
            PaymentsReceived = Dec(j, "paymentsReceived"),
            CurrentCharges = Dec(j, "currentCharges"),
            AmountDue = Dec(j, "amountDue")
        };

        var n = 1;
        foreach (var c in j.GetProperty("charges").EnumerateArray())
        {
            bill.Charges.Add(new UtilityCharge
            {
                LineNumber = n++,
                Description = Str(c, "description"),
                Amount = Dec(c, "amount")
            });
        }
        return bill;
    }

    // ---------- Entity -> JSON (same shape as the schema; bank amounts are signed again) ----------

    private static string Iso(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static JsonElement ToJson(Invoice i) => JsonSerializer.SerializeToElement(new
    {
        vendorName = i.VendorName,
        invoiceNumber = i.InvoiceNumber,
        invoiceDate = Iso(i.InvoiceDate),
        dueDate = i.DueDate is { } due ? Iso(due) : null,
        currency = i.Currency,
        billTo = i.BillTo,
        lineItems = i.Lines.OrderBy(l => l.LineNumber)
            .Select(l => new { description = l.Description, quantity = l.Quantity, unitPrice = l.UnitPrice, amount = l.Amount }),
        subtotal = i.Subtotal,
        tax = i.Tax,
        total = i.Total
    });

    public static JsonElement ToJson(BankStatement b) => JsonSerializer.SerializeToElement(new
    {
        institutionName = b.InstitutionName,
        accountHolder = b.AccountHolder,
        accountType = b.AccountType.ToString(),
        accountNumberMasked = b.AccountNumberMasked,
        periodStart = Iso(b.PeriodStart),
        periodEnd = Iso(b.PeriodEnd),
        currency = b.Currency,
        openingBalance = b.OpeningBalance,
        closingBalance = b.ClosingBalance,
        transactions = b.Transactions.OrderBy(t => t.LineNumber)
            .Select(t => new { date = Iso(t.TransactionDate), description = t.Description, amount = t.SignedAmount })
    });

    public static JsonElement ToJson(AchReturnNotice a) => JsonSerializer.SerializeToElement(new
    {
        originatorName = a.OriginatorName,
        receiverName = a.ReceiverName,
        rdfiName = a.RdfiName,
        secCode = a.SecCode,
        originalEffectiveDate = Iso(a.OriginalEffectiveDate),
        returnDate = Iso(a.ReturnDate),
        traceNumber = a.TraceNumber,
        originalTraceNumber = a.OriginalTraceNumber,
        amount = a.Amount,
        returnReasonCode = a.ReturnReasonCode,
        returnReasonDescription = a.ReturnReasonDescription
    });

    public static JsonElement ToJson(UtilityBill u) => JsonSerializer.SerializeToElement(new
    {
        providerName = u.ProviderName,
        utilityType = u.UtilityType.ToString(),
        accountNumberMasked = u.AccountNumberMasked,
        serviceAddress = u.ServiceAddress,
        statementDate = Iso(u.StatementDate),
        periodStart = Iso(u.PeriodStart),
        periodEnd = Iso(u.PeriodEnd),
        dueDate = Iso(u.DueDate),
        usage = new { quantity = u.UsageQuantity, unit = u.UsageUnit },
        charges = u.Charges.OrderBy(c => c.LineNumber).Select(c => new { description = c.Description, amount = c.Amount }),
        previousBalance = u.PreviousBalance,
        paymentsReceived = u.PaymentsReceived,
        currentCharges = u.CurrentCharges,
        amountDue = u.AmountDue
    });

    private static string Str(JsonElement e, string name) => e.GetProperty(name).GetString()!;

    private static string? OptStr(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static decimal Dec(JsonElement e, string name) => e.GetProperty(name).GetDecimal();

    private static DateOnly Date(JsonElement e, string name) =>
        DateOnly.ParseExact(Str(e, name), "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static DateOnly? OptDate(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? Date(e, name) : null;
}
