using System.Text;
using System.Text.Json;
using DocIntake.Api.Configuration;
using DocIntake.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace DocIntake.Tests;

public class DocumentTextExtractorTests(ITestOutputHelper output)
{
    private static readonly string TestDataDir = Path.Combine(AppContext.BaseDirectory, "TestData");

    private static DocumentTextExtractor Create(TextExtractionOptions? options = null) =>
        new(Options.Create(options ?? new TextExtractionOptions()), NullLogger<DocumentTextExtractor>.Instance);

    public static IEnumerable<object[]> SampleDocuments()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestDataDir, "manifest.json")));
        foreach (var doc in manifest.RootElement.GetProperty("documents").EnumerateArray())
            yield return [doc.GetProperty("file").GetString()!, doc.GetProperty("answer").GetString()!];
    }

    // One value that every layout of the type prints, so finding it proves the text was read.
    private static string Needle(JsonElement expected, string documentType) => documentType switch
    {
        "invoice" => expected.GetProperty("invoiceNumber").GetString()!,
        "bankStatement" => expected.GetProperty("accountHolder").GetString()!,
        "achReturnNotice" => expected.GetProperty("traceNumber").GetString()!,
        "utilityBill" => expected.GetProperty("serviceAddress").GetString()!,
        _ => throw new InvalidOperationException($"Unknown type {documentType}")
    };

    private static FileType TypeOf(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".pdf" => FileType.Pdf,
        ".docx" => FileType.Docx,
        ".txt" => FileType.Text,
        _ => FileType.Unknown
    };

    [Theory]
    [MemberData(nameof(SampleDocuments))]
    public async Task Extracts_the_identifying_value_from_every_sample(string file, string answer)
    {
        using var key = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestDataDir, answer)));
        var needle = Needle(key.RootElement.GetProperty("expected"), key.RootElement.GetProperty("documentType").GetString()!);

        await using var stream = File.OpenRead(Path.Combine(TestDataDir, file));
        var result = await Create().ExtractAsync(stream, TypeOf(file));

        Assert.Contains(needle, result.Text);
        Assert.False(result.Truncated);
    }

    [Fact]
    public async Task Truncates_to_the_character_cap()
    {
        var extractor = Create(new TextExtractionOptions { MaxCharacters = 100 });
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(new string('a', 500)));

        var result = await extractor.ExtractAsync(stream, FileType.Text);

        Assert.Equal(100, result.Text.Length);
        Assert.True(result.Truncated);
    }

    [Fact]
    public async Task Rejects_a_corrupt_pdf()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("this is not a pdf"));
        await Assert.ThrowsAsync<TextExtractionException>(() => Create().ExtractAsync(stream, FileType.Pdf));
    }

    [Fact]
    public async Task Rejects_an_unsupported_type()
    {
        await using var stream = new MemoryStream([1, 2, 3]);
        await Assert.ThrowsAsync<TextExtractionException>(() => Create().ExtractAsync(stream, FileType.Xlsx));
    }

    // Look-and-see test: prints what the extractor produced for one sample so you can judge the layout.
    // Change the path to inspect other documents. The sample data is synthetic, so printing it is fine.
    [Fact]
    public async Task Print_extracted_text_for_invoice_01()
    {
        await using var stream = File.OpenRead(Path.Combine(TestDataDir, "invoice", "invoice-01.pdf"));
        var result = await Create().ExtractAsync(stream, FileType.Pdf);

        output.WriteLine($"Method: {result.Method}, pages: {result.PageCount}, characters: {result.Text.Length}");
        output.WriteLine("-----");
        output.WriteLine(result.Text);
    }
}
