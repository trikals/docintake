using System.Text.Json;
using DocIntake.Api.Models;
using DocIntake.Api.Services;

namespace DocIntake.Tests;

public class ConfirmedDataMapperTests
{
    private static readonly string TestDataDir = Path.Combine(AppContext.BaseDirectory, "TestData");

    private static JsonDocument Expected(string folder, string name)
    {
        using var key = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestDataDir, folder, $"{name}.answer.json")));
        return JsonDocument.Parse(key.RootElement.GetProperty("expected").GetRawText());
    }

    [Fact]
    public void Maps_an_invoice_with_its_lines_in_order()
    {
        using var json = Expected("invoice", "invoice-01");

        var invoice = ConfirmedDataMapper.ToInvoice(7, json.RootElement);

        Assert.Equal(7, invoice.DocumentId);
        Assert.Equal(json.RootElement.GetProperty("lineItems").GetArrayLength(), invoice.Lines.Count);
        Assert.Equal(Enumerable.Range(1, invoice.Lines.Count), invoice.Lines.Select(l => l.LineNumber));
        Assert.Equal(json.RootElement.GetProperty("total").GetDecimal(), invoice.Total);
    }

    // Debit/credit direction plus positive amounts must reproduce the signed arithmetic of the statement.
    [Theory]
    [InlineData("bank-statement-01")]
    [InlineData("bank-statement-02")]
    [InlineData("bank-statement-03")]
    [InlineData("bank-statement-04")]
    public void Bank_transactions_become_direction_plus_positive_amount(string name)
    {
        using var json = Expected("bank-statement", name);

        var statement = ConfirmedDataMapper.ToBankStatement(1, json.RootElement);

        Assert.All(statement.Transactions, t => Assert.True(t.Amount >= 0));
        Assert.Contains(statement.Transactions, t => t.Direction == TransactionDirection.Debit);
        Assert.Equal(statement.ClosingBalance - statement.OpeningBalance, statement.Transactions.Sum(t => t.SignedAmount));
        Assert.True(Enum.IsDefined(statement.AccountType));
    }

    [Fact]
    public void Maps_an_ach_return_notice()
    {
        using var json = Expected("ach-return", "ach-return-01");

        var notice = ConfirmedDataMapper.ToAchReturnNotice(3, json.RootElement);

        Assert.Equal(15, notice.TraceNumber.Length);
        Assert.Equal(json.RootElement.GetProperty("returnReasonCode").GetString(), notice.ReturnReasonCode);
        Assert.Equal(DateOnly.Parse(json.RootElement.GetProperty("returnDate").GetString()!), notice.ReturnDate);
    }

    [Fact]
    public void Maps_a_utility_bill_with_its_charges()
    {
        using var json = Expected("utility-bill", "utility-bill-01");

        var bill = ConfirmedDataMapper.ToUtilityBill(4, json.RootElement);

        Assert.Equal(json.RootElement.GetProperty("charges").GetArrayLength(), bill.Charges.Count);
        Assert.Equal(bill.PreviousBalance - bill.PaymentsReceived + bill.CurrentCharges, bill.AmountDue);
    }

    [Theory]
    [InlineData("invoice", "invoice-05")]
    [InlineData("invoice", "invoice-06")]
    [InlineData("utility-bill", "utility-bill-05")]
    [InlineData("utility-bill", "utility-bill-06")]
    public void Flawed_but_well_formed_documents_still_map(string folder, string name)
    {
        // Flawed values are acknowledgeable issues, not structural errors, so they must be saveable as printed.
        using var json = Expected(folder, name);

        object entity = folder == "invoice"
            ? ConfirmedDataMapper.ToInvoice(1, json.RootElement)
            : ConfirmedDataMapper.ToUtilityBill(1, json.RootElement);

        Assert.NotNull(entity);
    }
}
