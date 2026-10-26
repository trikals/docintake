/// <summary>Where a document is in the workflow. See the design doc for the allowed transitions.</summary>
public enum DocumentStatus
{
    Received = 0,
    Extracted,
    NeedsReview,
    Confirmed,
    Failed
}
