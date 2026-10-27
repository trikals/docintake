namespace DocIntake.Api.Configuration;

public class QuotaOptions
{
    /// <summary>Extractions one user may run per UTC day. Each one can call a paid model.</summary>
    public int DailyExtractions { get; set; } = 20;
}
