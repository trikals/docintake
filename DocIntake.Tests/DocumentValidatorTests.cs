using System.Text.Json;
using DocIntake.Api.Services;
using DocIntake.Api.Services.Validation;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocIntake.Tests;

public class DocumentValidatorTests
{
    private static readonly string TestDataDir = Path.Combine(AppContext.BaseDirectory, "TestData");

    public static IEnumerable<object[]> SampleDocuments() => DocumentTextExtractorTests.SampleDocuments();

    // The answer key holds the values as printed, plus the issues the rules must raise.
    // Clean samples must raise nothing; the 8 flawed ones must raise exactly what the key lists.
    [Theory]
    [MemberData(nameof(SampleDocuments))]
    public void Raises_exactly_the_issues_in_the_answer_key(string file, string answer)
    {
        using var key = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestDataDir, answer)));
        var type = Enum.Parse<DocumentType>(key.RootElement.GetProperty("documentType").GetString()!, ignoreCase: true);
        var expectedIssues = key.RootElement.GetProperty("expectedIssues").EnumerateArray()
            .Select(i => (Rule: i.GetProperty("rule").GetString()!, Path: i.GetProperty("path").GetString()!))
            .OrderBy(i => i.Rule).ThenBy(i => i.Path).ToList();

        var actual = DocumentValidator.Validate(type, key.RootElement.GetProperty("expected").GetRawText())
            .Select(i => (i.Rule, i.Path))
            .OrderBy(i => i.Rule).ThenBy(i => i.Path).ToList();

        Assert.Equal(expectedIssues, actual);
    }

    [Theory]
    [InlineData(DocumentType.Invoice)]
    [InlineData(DocumentType.BankStatement)]
    [InlineData(DocumentType.AchReturnNotice)]
    [InlineData(DocumentType.UtilityBill)]
    public async Task Stub_output_is_valid(DocumentType type)
    {
        var stub = new StubDocumentExtractor(NullLogger<StubDocumentExtractor>.Instance);
        var result = await stub.ExtractAsync(type, "ignored");

        Assert.Empty(DocumentValidator.Validate(type, result.Json));
    }

    [Fact]
    public void Reports_invalid_json()
    {
        var issues = DocumentValidator.Validate(DocumentType.Invoice, "not json");
        Assert.Contains(issues, i => i.Rule == "InvalidJson");
    }

    [Fact]
    public void Reports_missing_fields_and_skips_dependent_rules()
    {
        var issues = DocumentValidator.Validate(DocumentType.Invoice, """{ "subtotal": 10, "tax": 1 }""");

        Assert.Contains(issues, i => i is { Rule: "MissingField", Path: "total" });
        Assert.DoesNotContain(issues, i => i.Rule == "TotalMismatch");
    }

    [Fact]
    public void Refuses_to_validate_an_unknown_type()
        => Assert.Throws<InvalidOperationException>(() => DocumentValidator.Validate(DocumentType.Unknown, "{}"));
}
