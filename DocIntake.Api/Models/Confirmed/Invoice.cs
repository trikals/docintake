namespace DocIntake.Api.Models;

/// <summary>A person-confirmed invoice. One per document.</summary>
public class Invoice
{
    public int Id { get; set; }

    /// <summary>The document this was confirmed from. One confirmed invoice per document.</summary>
    public int DocumentId { get; set; }
    public Document Document { get; set; } = null!;

    public string VendorName { get; set; } = null!;
    public string InvoiceNumber { get; set; } = null!;
    public DateOnly InvoiceDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public string Currency { get; set; } = null!;
    public string? BillTo { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }

    public List<InvoiceLine> Lines { get; set; } = [];
}

public class InvoiceLine
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }

    /// <summary>Position on the invoice, starting at 1, so the original order is kept.</summary>
    public int LineNumber { get; set; }

    public string Description { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}
