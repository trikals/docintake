using System.Text.Json;
using DocIntake.Api.Models;
using DocIntake.Api.Services;

namespace DocIntake.Tests;

public class ConfirmedDataRoundTripTests
{
    private static readonly string TestDataDir = Path.Combine(AppContext.BaseDirectory, "TestData");

    public static IEnumerable<object[]> SampleDocuments() => DocumentTextExtractorTests.SampleDocuments();

    // JSON -> entities -> JSON must give back what went in: nothing lost, nothing invented, signs restored.
    [Theory]
    [MemberData(nameof(SampleDocuments))]
    public void Saved_data_reads_back_unchanged(string file, string answer)
    {
        using var key = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestDataDir, answer)));
        var type = Enum.Parse<DocumentType>(key.RootElement.GetProperty("documentType").GetString()!, ignoreCase: true);
        using var original = JsonDocument.Parse(key.RootElement.GetProperty("expected").GetRawText());
        var input = original.RootElement;

        var output = type switch
        {
            DocumentType.Invoice => ConfirmedDataMapper.ToJson(ConfirmedDataMapper.ToInvoice(1, input)),
            DocumentType.BankStatement => ConfirmedDataMapper.ToJson(ConfirmedDataMapper.ToBankStatement(1, input)),
            DocumentType.AchReturnNotice => ConfirmedDataMapper.ToJson(ConfirmedDataMapper.ToAchReturnNotice(1, input)),
            DocumentType.UtilityBill => ConfirmedDataMapper.ToJson(ConfirmedDataMapper.ToUtilityBill(1, input)),
            _ => throw new InvalidOperationException()
        };

        AssertSameJson(input, output, "$");
    }

    // Structural comparison: same properties, same array order, numbers compared by value (1158.0 equals 1158.00).
    private static void AssertSameJson(JsonElement expected, JsonElement actual, string path)
    {
        Assert.True(expected.ValueKind == actual.ValueKind, $"{path}: expected {expected.ValueKind} but was {actual.ValueKind}");

        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                Assert.Equal(
                    expected.EnumerateObject().Select(p => p.Name).Order(),
                    actual.EnumerateObject().Select(p => p.Name).Order());
                foreach (var p in expected.EnumerateObject())
                    AssertSameJson(p.Value, actual.GetProperty(p.Name), $"{path}.{p.Name}");
                break;
            case JsonValueKind.Array:
                Assert.True(expected.GetArrayLength() == actual.GetArrayLength(), $"{path}: array length differs");
                var e = expected.EnumerateArray().ToList();
                var a = actual.EnumerateArray().ToList();
                for (var i = 0; i < e.Count; i++)
                    AssertSameJson(e[i], a[i], $"{path}[{i}]");
                break;
            case JsonValueKind.Number:
                Assert.True(expected.GetDecimal() == actual.GetDecimal(), $"{path}: {expected.GetDecimal()} != {actual.GetDecimal()}");
                break;
            default:
                Assert.True(expected.ToString() == actual.ToString(), $"{path}: values differ");
                break;
        }
    }
}
