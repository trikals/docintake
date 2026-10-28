using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DocIntake.Api.Models;

namespace DocIntake.Tests.Integration;

/// <summary>Proves the security promises of the API against a real database.</summary>
[Collection(ApiCollection.Name)]
public class OwnershipTests(ApiFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private HttpClient ClientFor(string? subject, params string[] scopes)
    {
        var client = factory.CreateClient();
        if (subject is not null)
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", TestJwt.Create(subject, scopes));
        return client;
    }

    private static HttpClient WithBothScopes(ApiFactory f, string subject)
    {
        var client = f.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", TestJwt.Create(subject, "documents.read", "documents.write"));
        return client;
    }

    private static async Task<DocumentResponse> UploadTextAsync(HttpClient client)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes("INVOICE\nInvoice number: TEST-1\nTotal due: 10.00\n"));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        form.Add(file, "file", "sample.txt");

        var response = await client.PostAsync("/documents", form);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<DocumentResponse>(Json))!;
    }

    [Fact]
    public async Task Request_without_a_token_is_rejected()
    {
        var response = await ClientFor(null).GetAsync("/documents");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Read_only_token_cannot_upload()
    {
        var client = ClientFor("reader-user", "documents.read");
        using var form = new MultipartFormDataContent
        {
            { new ByteArrayContent(Encoding.UTF8.GetBytes("hello")), "file", "a.txt" }
        };

        var response = await client.PostAsync("/documents", form);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task A_user_cannot_see_or_change_another_users_document()
    {
        var alice = WithBothScopes(factory, "alice");
        var bob = WithBothScopes(factory, "bob");

        var doc = await UploadTextAsync(alice);

        // The owner can read it.
        Assert.Equal(HttpStatusCode.OK, (await alice.GetAsync($"/documents/{doc.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await alice.GetAsync($"/documents/{doc.Id}/file")).StatusCode);

        // Another user gets "not found" for every operation, never "forbidden", so ids leak nothing.
        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync($"/documents/{doc.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync($"/documents/{doc.Id}/file")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.PostAsync($"/documents/{doc.Id}/extract", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.DeleteAsync($"/documents/{doc.Id}")).StatusCode);

        // The other user's list does not contain it, and the owner's still does.
        var bobList = await bob.GetFromJsonAsync<PagedResponse<DocumentResponse>>("/documents", Json);
        Assert.DoesNotContain(bobList!.Items, d => d.Id == doc.Id);

        var aliceList = await alice.GetFromJsonAsync<PagedResponse<DocumentResponse>>("/documents", Json);
        Assert.Contains(aliceList!.Items, d => d.Id == doc.Id);

        // Bob's failed delete really changed nothing.
        Assert.Equal(HttpStatusCode.OK, (await alice.GetAsync($"/documents/{doc.Id}")).StatusCode);
    }
}
