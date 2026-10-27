namespace DocIntake.Api.Models;

/// <summary>
/// How many extractions a user has run on a given UTC day. Kept separately from Extraction on purpose:
/// deleting a document removes its extractions, and that must not reset the daily cap.
/// </summary>
public class UsageDay
{
    public int Id { get; set; }
    public string OwnerId { get; set; } = null!;
    public DateOnly Day { get; set; }
    public int ExtractionCount { get; set; }
}
