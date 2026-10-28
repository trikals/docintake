using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DocIntake.Api.Models;

namespace DocIntake.Tests.Integration;

[Collection(ApiCollection.Name)]
public class QuotaAndHealthTests(ApiFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private HttpClient ClientFor(string subject)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", TestJwt.Create(subject, "documents.read", "documents.write"));
        return client;
    }

    private static async Task<int> UploadTextAsync(HttpClient client)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes("INVOICE\nInvoice number: Q-1\nTotal due: 5.00\n"));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        form.Add(file, "file", "quota.txt");

        var response = await client.PostAsync("/documents", form);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<DocumentResponse>(Json))!.Id;
    }

    [Fact]
    public async Task Extraction_stops_at_the_daily_limit_and_other_users_are_not_affected()
    {
        var carol = ClientFor("carol");
        var id = await UploadTextAsync(carol);

        // The first QuotaPerDay extractions are allowed.
        for (var i = 0; i < ApiFactory.QuotaPerDay; i++)
            Assert.Equal(HttpStatusCode.OK, (await carol.PostAsync($"/documents/{id}/extract", null)).StatusCode);

        // The next one is refused, and tells the caller when to come back.
        var refused = await carol.PostAsync($"/documents/{id}/extract", null);
        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
        Assert.True(refused.Headers.RetryAfter is not null);

        // Another user has their own allowance.
        var dave = ClientFor("dave");
        var daveDoc = await UploadTextAsync(dave);
        Assert.Equal(HttpStatusCode.OK, (await dave.PostAsync($"/documents/{daveDoc}/extract", null)).StatusCode);
    }

    [Fact]
    public async Task Deleting_documents_does_not_reset_the_daily_limit()
    {
        var erin = ClientFor("erin");

        for (var i = 0; i < ApiFactory.QuotaPerDay; i++)
        {
            var id = await UploadTextAsync(erin);
            Assert.Equal(HttpStatusCode.OK, (await erin.PostAsync($"/documents/{id}/extract", null)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await erin.DeleteAsync($"/documents/{id}")).StatusCode);
        }

        // All earlier documents and their extraction rows are gone, but the allowance stays used up.
        var fresh = await UploadTextAsync(erin);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await erin.PostAsync($"/documents/{fresh}/extract", null)).StatusCode);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/ready")]
    public async Task Health_endpoints_answer_without_a_token(string path)
    {
        var response = await factory.CreateClient().GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
