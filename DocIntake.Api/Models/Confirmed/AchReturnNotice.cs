namespace DocIntake.Api.Models;

/// <summary>A person-confirmed ACH return notice. One per document.</summary>
public class AchReturnNotice
{
    public int Id { get; set; }

    /// <summary>The document this was confirmed from. One confirmed notice per document.</summary>
    public int DocumentId { get; set; }
    public Document Document { get; set; } = null!;

    public string OriginatorName { get; set; } = null!;
    public string ReceiverName { get; set; } = null!;
    public string RdfiName { get; set; } = null!;
    public string SecCode { get; set; } = null!;
    public DateOnly OriginalEffectiveDate { get; set; }
    public DateOnly ReturnDate { get; set; }
    public string TraceNumber { get; set; } = null!;
    public string OriginalTraceNumber { get; set; } = null!;
    public decimal Amount { get; set; }
    public string ReturnReasonCode { get; set; } = null!;
    public string ReturnReasonDescription { get; set; } = null!;
}
