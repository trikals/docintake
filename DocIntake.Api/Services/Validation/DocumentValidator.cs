using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DocIntake.Api.Services.Validation;

/// <summary>
/// Deterministic checks on extracted data. The model proposes; this code verifies.
/// Pure functions: no I/O, no logging of values. Money is compared to the cent.
/// </summary>
public static partial class DocumentValidator
{
    private const decimal Tolerance = 0.005m;

    private static readonly Dictionary<string, string> AchReturnCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["R01"] = "Insufficient Funds",
        ["R02"] = "Account Closed",
        ["R03"] = "No Account/Unable to Locate Account",
        ["R04"] = "Invalid Account Number Structure",
        ["R07"] = "Authorization Revoked by Customer",
        ["R08"] = "Payment Stopped",
        ["R09"] = "Uncollected Funds",
        ["R10"] = "Customer Advises Not Authorized",
        ["R16"] = "Account Frozen",
        ["R20"] = "Non-Transaction Account",
        ["R29"] = "Corporate Customer Advises Not Authorized"
    };

    private static readonly string[] UtilityTypes = ["Electric", "Gas", "Water", "Other"];

    [GeneratedRegex(@"^\d{15}$")]
    private static partial Regex FifteenDigits();

    public static IReadOnlyList<ValidationIssue> Validate(DocumentType documentType, string json)
    {
        JsonDocument doc;
        try { doc = JsonDocument.Parse(json); }
        catch (JsonException) { return [new("InvalidJson", "", "The extracted data is not valid JSON.")]; }

        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return [new("InvalidJson", "", "The extracted data is not a JSON object.")];

            var issues = new List<ValidationIssue>();
            var r = new Reader(doc.RootElement, issues);
            switch (documentType)
            {
                case DocumentType.Invoice: Invoice(r); break;
                case DocumentType.BankStatement: BankStatement(r); break;
                case DocumentType.AchReturnNotice: AchReturn(r); break;
                case DocumentType.UtilityBill: UtilityBill(r); break;
                default: throw new InvalidOperationException($"Cannot validate a document of type {documentType}.");
            }
            return issues;
        }
    }

    // ---------- Invoice ----------
    private static void Invoice(Reader r)
    {
        var root = r.Root;
        r.Str(root, "", "vendorName");
        r.Str(root, "", "invoiceNumber");
        r.Str(root, "", "currency");
        var invoiceDate = r.Date(root, "", "invoiceDate");
        var dueDate = r.Date(root, "", "dueDate", required: false);

        decimal lineSum = 0;
        var linesComplete = true;
        var items = r.Array(root, "lineItems");
        for (var i = 0; i < items.Count; i++)
        {
            var prefix = $"lineItems[{i}].";
            r.Str(items[i], prefix, "description");
            var qty = r.Decimal(items[i], prefix, "quantity");
            var unitPrice = r.Decimal(items[i], prefix, "unitPrice");
            var amount = r.Decimal(items[i], prefix, "amount");

            if (qty is not null && unitPrice is not null && amount is not null && Differs(qty.Value * unitPrice.Value, amount.Value))
                r.Add("LineAmountMismatch", prefix + "amount",
                    $"Quantity × unit price is {Money(qty.Value * unitPrice.Value)} but the line amount is {Money(amount.Value)}.");

            if (amount is null) linesComplete = false; else lineSum += amount.Value;
        }

        var subtotal = r.Decimal(root, "", "subtotal");
        var tax = r.Decimal(root, "", "tax");
        var total = r.Decimal(root, "", "total");

        if (linesComplete && subtotal is not null && Differs(lineSum, subtotal.Value))
            r.Add("SubtotalMismatch", "subtotal", $"Line amounts add up to {Money(lineSum)} but the subtotal is {Money(subtotal.Value)}.");

        if (subtotal is not null && tax is not null && total is not null && Differs(subtotal.Value + tax.Value, total.Value))
            r.Add("TotalMismatch", "total", $"Subtotal plus tax is {Money(subtotal.Value + tax.Value)} but the total is {Money(total.Value)}.");

        if (invoiceDate is not null && dueDate is not null && dueDate < invoiceDate)
            r.Add("DueBeforeInvoice", "dueDate", "The due date is before the invoice date.");
    }

    // ---------- Bank statement ----------
    private static void BankStatement(Reader r)
    {
        var root = r.Root;
        r.Str(root, "", "institutionName");
        r.Str(root, "", "accountHolder");
        r.Str(root, "", "accountNumberMasked");
        r.Str(root, "", "currency");
        var start = r.Date(root, "", "periodStart");
        var end = r.Date(root, "", "periodEnd");
        var opening = r.Decimal(root, "", "openingBalance");
        var closing = r.Decimal(root, "", "closingBalance");

        if (start is not null && end is not null && start > end)
            r.Add("PeriodInvalid", "periodStart", "The period start is after the period end.");

        decimal sum = 0;
        var complete = true;
        var transactions = r.Array(root, "transactions");
        for (var i = 0; i < transactions.Count; i++)
        {
            var prefix = $"transactions[{i}].";
            var date = r.Date(transactions[i], prefix, "date");
            r.Str(transactions[i], prefix, "description");
            var amount = r.Decimal(transactions[i], prefix, "amount");

            if (amount is null) complete = false; else sum += amount.Value;

            if (date is not null && start is not null && end is not null && (date < start || date > end))
                r.Add("TransactionOutsidePeriod", prefix + "date", "This transaction is dated outside the statement period.");
        }

        if (complete && opening is not null && closing is not null && Differs(opening.Value + sum, closing.Value))
            r.Add("BalanceMismatch", "closingBalance",
                $"Opening balance plus transactions is {Money(opening.Value + sum)} but the closing balance is {Money(closing.Value)}.");
    }

    // ---------- ACH return notice ----------
    private static void AchReturn(Reader r)
    {
        var root = r.Root;
        r.Str(root, "", "originatorName");
        r.Str(root, "", "receiverName");
        r.Str(root, "", "rdfiName");
        r.Str(root, "", "secCode");
        var effective = r.Date(root, "", "originalEffectiveDate");
        var returned = r.Date(root, "", "returnDate");
        var trace = r.Str(root, "", "traceNumber");
        var originalTrace = r.Str(root, "", "originalTraceNumber");
        var amount = r.Decimal(root, "", "amount");
        var code = r.Str(root, "", "returnReasonCode");
        var description = r.Str(root, "", "returnReasonDescription");

        if (trace is not null && !FifteenDigits().IsMatch(trace))
            r.Add("InvalidTraceNumber", "traceNumber", "A trace number must be exactly 15 digits.");
        if (originalTrace is not null && !FifteenDigits().IsMatch(originalTrace))
            r.Add("InvalidTraceNumber", "originalTraceNumber", "A trace number must be exactly 15 digits.");

        if (amount is not null && amount <= 0)
            r.Add("NonPositiveAmount", "amount", "The amount must be greater than zero.");

        if (effective is not null && returned is not null && returned < effective)
            r.Add("ReturnBeforeEffective", "returnDate", "The return date is before the original effective date.");

        if (code is not null)
        {
            if (!AchReturnCodes.TryGetValue(code.Trim(), out var expectedDescription))
                r.Add("InvalidReturnCode", "returnReasonCode", $"'{code}' is not a recognized return reason code.");
            else if (description is not null && !string.Equals(description.Trim(), expectedDescription, StringComparison.OrdinalIgnoreCase))
                r.Add("ReasonDescriptionMismatch", "returnReasonDescription",
                    $"Code {code.Trim().ToUpperInvariant()} means '{expectedDescription}', not '{description}'.");
        }
    }

    // ---------- Utility bill ----------
    private static void UtilityBill(Reader r)
    {
        var root = r.Root;
        r.Str(root, "", "providerName");
        r.Str(root, "", "accountNumberMasked");
        r.Str(root, "", "serviceAddress");

        var utilityType = r.Str(root, "", "utilityType");
        if (utilityType is not null && !UtilityTypes.Contains(utilityType))
            r.Add("InvalidValue", "utilityType", "Utility type must be Electric, Gas, Water or Other.");

        var statement = r.Date(root, "", "statementDate");
        var start = r.Date(root, "", "periodStart");
        var end = r.Date(root, "", "periodEnd");
        var due = r.Date(root, "", "dueDate");

        if (root.TryGetProperty("usage", out var usage) && usage.ValueKind == JsonValueKind.Object)
        {
            r.Decimal(usage, "usage.", "quantity");
            r.Str(usage, "usage.", "unit");
        }
        else r.Add("MissingField", "usage", "A required field is missing.");

        decimal chargeSum = 0;
        var complete = true;
        var charges = r.Array(root, "charges");
        for (var i = 0; i < charges.Count; i++)
        {
            var prefix = $"charges[{i}].";
            r.Str(charges[i], prefix, "description");
            var amount = r.Decimal(charges[i], prefix, "amount");
            if (amount is null) complete = false; else chargeSum += amount.Value;
        }

        var previous = r.Decimal(root, "", "previousBalance");
        var paid = r.Decimal(root, "", "paymentsReceived");
        var current = r.Decimal(root, "", "currentCharges");
        var amountDue = r.Decimal(root, "", "amountDue");

        if (complete && current is not null && Differs(chargeSum, current.Value))
            r.Add("ChargesSumMismatch", "currentCharges", $"The charges add up to {Money(chargeSum)} but current charges are {Money(current.Value)}.");

        if (previous is not null && paid is not null && current is not null && amountDue is not null
            && Differs(previous.Value - paid.Value + current.Value, amountDue.Value))
            r.Add("AmountDueMismatch", "amountDue",
                $"Previous balance minus payments plus current charges is {Money(previous.Value - paid.Value + current.Value)} but the amount due is {Money(amountDue.Value)}.");

        if (statement is not null && due is not null && due <= statement)
            r.Add("DueBeforeStatement", "dueDate", "The due date must be after the statement date.");

        if (start is not null && end is not null && start >= end)
            r.Add("PeriodInvalid", "periodStart", "The period start must be before the period end.");
    }

    private static bool Differs(decimal a, decimal b) => Math.Abs(a - b) > Tolerance;
    private static string Money(decimal d) => d.ToString("0.00", CultureInfo.InvariantCulture);

    /// <summary>Reads typed fields and records MissingField / InvalidValue issues, returning null so dependent rules are skipped.</summary>
    private sealed class Reader(JsonElement root, List<ValidationIssue> issues)
    {
        public JsonElement Root => root;

        public void Add(string rule, string path, string message) => issues.Add(new ValidationIssue(rule, path, message));

        private bool TryGet(JsonElement obj, string prefix, string name, bool required, out JsonElement value)
        {
            if (obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty(name, out value) && value.ValueKind != JsonValueKind.Null)
                return true;
            value = default;
            if (required) Add("MissingField", prefix + name, "A required field is missing.");
            return false;
        }

        public string? Str(JsonElement obj, string prefix, string name, bool required = true)
        {
            if (!TryGet(obj, prefix, name, required, out var v)) return null;
            if (v.ValueKind != JsonValueKind.String) { Add("InvalidValue", prefix + name, "Expected text."); return null; }
            return v.GetString();
        }

        public decimal? Decimal(JsonElement obj, string prefix, string name, bool required = true)
        {
            if (!TryGet(obj, prefix, name, required, out var v)) return null;
            if (v.ValueKind != JsonValueKind.Number || !v.TryGetDecimal(out var d)) { Add("InvalidValue", prefix + name, "Expected a number."); return null; }
            return d;
        }

        public DateOnly? Date(JsonElement obj, string prefix, string name, bool required = true)
        {
            var s = Str(obj, prefix, name, required);
            if (s is null) return null;
            if (!DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            { Add("InvalidValue", prefix + name, "Expected a date as yyyy-MM-dd."); return null; }
            return d;
        }

        public List<JsonElement> Array(JsonElement obj, string name)
        {
            if (!TryGet(obj, "", name, true, out var v)) return [];
            if (v.ValueKind != JsonValueKind.Array) { Add("InvalidValue", name, "Expected a list."); return []; }
            return v.EnumerateArray().ToList();
        }
    }
}
