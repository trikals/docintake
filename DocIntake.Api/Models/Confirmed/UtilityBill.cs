namespace DocIntake.Api.Models;

/// <summary>A person-confirmed utility bill. One per document.</summary>
public class UtilityBill
{
    public int Id { get; set; }

    /// <summary>The document this was confirmed from. One confirmed bill per document.</summary>
    public int DocumentId { get; set; }
    public Document Document { get; set; } = null!;

    public string ProviderName { get; set; } = null!;
    public UtilityType UtilityType { get; set; }

    /// <summary>Last four digits only.</summary>
    public string AccountNumberMasked { get; set; } = null!;

    public string ServiceAddress { get; set; } = null!;
    public DateOnly StatementDate { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal UsageQuantity { get; set; }
    public string UsageUnit { get; set; } = null!;
    public decimal PreviousBalance { get; set; }
    public decimal PaymentsReceived { get; set; }
    public decimal CurrentCharges { get; set; }
    public decimal AmountDue { get; set; }

    public List<UtilityCharge> Charges { get; set; } = [];
}

public class UtilityCharge
{
    public int Id { get; set; }
    public int UtilityBillId { get; set; }
    public int LineNumber { get; set; }
    public string Description { get; set; } = null!;
    public decimal Amount { get; set; }
}
