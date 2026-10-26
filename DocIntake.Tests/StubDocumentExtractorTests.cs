using System.Text.Json;
using DocIntake.Api.Configuration;
using DocIntake.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DocIntake.Tests;

public class StubDocumentExtractorTests
{
    private static readonly string TestDataDir = Path.Combine(AppContext.BaseDirectory, "TestData");

    private static readonly StubDocumentExtractor Stub = new(NullLogger<StubDocumentExtractor>.Instance);

    public static IEnumerable<object[]> SampleDocuments() => DocumentTextExtractorTests.SampleDocuments();

    // Real extracted text from every sample, so the keyword rules are checked against the actual layouts.
    [Theory]
    [MemberData(nameof(SampleDocuments))]
    public async Task Classifies_every_sample_correctly(string file, string answer)
    {
        using var key = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestDataDir, answer)));
        var expected = Enum.Parse<DocumentType>(key.RootElement.GetProperty("documentType").GetString()!, ignoreCase: true);

        var extractor = new DocumentTextExtractor(Options.Create(new TextExtractionOptions()), NullLogger<DocumentTextExtractor>.Instance);
        var fileType = Path.GetExtension(file).ToLowerInvariant() switch { ".pdf" => FileType.Pdf, ".docx" => FileType.Docx, _ => FileType.Text };
        await using var stream = File.OpenRead(Path.Combine(TestDataDir, file));
        var text = (await extractor.ExtractAsync(stream, fileType)).Text;

        Assert.Equal(expected, await Stub.ClassifyAsync(text));
    }

    [Theory]
    [InlineData(DocumentType.Invoice, "total")]
    [InlineData(DocumentType.BankStatement, "closingBalance")]
    [InlineData(DocumentType.AchReturnNotice, "returnReasonCode")]
    [InlineData(DocumentType.UtilityBill, "amountDue")]
    public async Task Returns_valid_json_for_each_type(DocumentType type, string expectedProperty)
    {
        var result = await Stub.ExtractAsync(type, "ignored");

        using var json = JsonDocument.Parse(result.Json);
        Assert.True(json.RootElement.TryGetProperty(expectedProperty, out _));
        Assert.Equal("stub.v1", result.PromptVersion);
    }

    [Fact]
    public async Task Refuses_to_extract_an_unknown_type()
        => await Assert.ThrowsAsync<InvalidOperationException>(() => Stub.ExtractAsync(DocumentType.Unknown, "x"));
}
