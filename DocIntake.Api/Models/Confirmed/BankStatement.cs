namespace DocIntake.Api.Models;

/// <summary>A person-confirmed bank statement. One per document.</summary>
public class BankStatement
{
    public int Id { get; set; }

    /// <summary>The document this was confirmed from. One confirmed statement per document.</summary>
    public int DocumentId { get; set; }
    public Document Document { get; set; } = null!;

    public string InstitutionName { get; set; } = null!;
    public string AccountHolder { get; set; } = null!;
    public AccountType AccountType { get; set; }

    /// <summary>Last four digits only. Full account numbers are never stored.</summary>
    public string AccountNumberMasked { get; set; } = null!;

    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public string Currency { get; set; } = null!;
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }

    public List<BankTransaction> Transactions { get; set; } = [];
}

public class BankTransaction
{
    public int Id { get; set; }
    public int BankStatementId { get; set; }
    public int LineNumber { get; set; }
    public DateOnly TransactionDate { get; set; }
    public string Description { get; set; } = null!;

    /// <summary>Credit = money in, Debit = money out.</summary>
    public TransactionDirection Direction { get; set; }

    /// <summary>Always positive; the direction says which way the money moved.</summary>
    public decimal Amount { get; set; }

    /// <summary>Amount with its sign: negative for a debit. Not stored.</summary>
    public decimal SignedAmount => Direction == TransactionDirection.Debit ? -Amount : Amount;
}
