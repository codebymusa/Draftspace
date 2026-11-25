using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DraftSpace;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Xunit;

public sealed class DocumentTests
{
    private const string RichText = """
        {"type":"doc","content":[{"type":"heading","attrs":{"level":2},"content":[{"type":"text","text":"Hello","marks":[{"type":"bold"},{"type":"underline"}]}]}]}
        """;

    [Fact]
    public async Task CreatedDocumentAndFormattingSurviveHostRestart()
    {
        var database = $"draftspace_test_{Guid.NewGuid():N}";
        try
        {
            Guid id;
            using (var app = Host(database))
            using (var alex = Client(app, 1))
            {
                id = await Create(alex);
                var result = await alex.PutAsJsonAsync(PathFor(id), SaveBody());
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                Assert.Equal(HttpStatusCode.NoContent, (await alex.PutAsync(PathFor(id) + "/shares/2", null)).StatusCode);
            }
            using var restarted = Host(database);
            using var owner = Client(restarted, 1);
            using var collaborator = Client(restarted, 2);
            var view = await owner.GetFromJsonAsync<JsonElement>(PathFor(id));
            Assert.Equal("Release notes", view.GetProperty("title").GetString());
            Assert.True(view.GetProperty("isOwner").GetBoolean());
            Assert.True(JsonElement.DeepEquals(JsonSerializer.Deserialize<JsonElement>(RichText), view.GetProperty("content")));
            Assert.Equal(HttpStatusCode.OK, (await collaborator.GetAsync(PathFor(id))).StatusCode);
        }
        finally { await Drop(database); }
    }

    [Fact]
    public async Task OnlyOwnersAndRecipientsCanEditAndOnlyOwnerManagesShares()
    {
        var database = $"draftspace_test_{Guid.NewGuid():N}";
        try
        {
            using var app = Host(database);
            using var owner = Client(app, 1);
            using var recipient = Client(app, 2);
            using var outsider = Client(app, 3);
            using var anonymous = Client(app, null);
            var path = PathFor(await Create(owner));
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await outsider.PutAsJsonAsync(path, SaveBody())).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await outsider.PutAsync(path + "/shares/2", null)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await owner.PutAsync(path + "/shares/2", null)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await owner.PutAsync(path + "/shares/2", null)).StatusCode);
            var shared = await recipient.GetFromJsonAsync<JsonElement[]>("/api/documents");
            Assert.False(Assert.Single(shared!).GetProperty("isOwner").GetBoolean());
            Assert.Equal(HttpStatusCode.OK, (await recipient.PutAsJsonAsync(path, SaveBody())).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await recipient.PutAsync(path + "/shares/3", null)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await recipient.DeleteAsync(path + "/shares/2")).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync(path + "/shares/2")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await recipient.GetAsync(path)).StatusCode);
        }
        finally { await Drop(database); }
    }

    [Theory]
    [InlineData("notes.pdf", "type")]
    [InlineData("notes.txt", "encoding")]
    [InlineData("notes.txt", "size")]
    public async Task InvalidImportDoesNotCreateDocument(string name, string reason)
    {
        var database = $"draftspace_test_{Guid.NewGuid():N}";
        try
        {
            using var app = Host(database);
            using var owner = Client(app, 1);
            byte[] bytes = reason switch { "encoding" => [0xff], "size" => new byte[262145], _ => Encoding.UTF8.GetBytes("hello") };
            using var form = new MultipartFormDataContent();
            form.Add(new ByteArrayContent(bytes), "file", name);
            Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsync("/api/documents/import", form)).StatusCode);
            Assert.Empty((await owner.GetFromJsonAsync<JsonElement[]>("/api/documents"))!);
        }
        finally { await Drop(database); }
    }

    [Fact]
    public async Task Utf8ImportIsEditableAndInvalidSaveIsRejected()
    {
        var database = $"draftspace_test_{Guid.NewGuid():N}";
        try
        {
            using var app = Host(database);
            using var owner = Client(app, 1);
            using var form = new MultipartFormDataContent();
            form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("Café\r\nnotes")), "file", "notes.TXT");
            var result = await owner.PostAsync("/api/documents/import", form);
            Assert.Equal(HttpStatusCode.Created, result.StatusCode);
            var view = await result.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("notes", view.GetProperty("title").GetString());
            Assert.Equal("Café\nnotes", view.GetProperty("content").GetProperty("content")[0]
                .GetProperty("content")[0].GetProperty("text").GetString());
            var path = PathFor(view.GetProperty("id").GetGuid());
            Assert.Equal(HttpStatusCode.BadRequest, (await owner.PutAsJsonAsync(path, new { title = " ", content = new { } })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync(path, SaveBody())).StatusCode);
        }
        finally { await Drop(database); }
    }

    private static WebApplicationFactory<Program> Host(string database) => new WebApplicationFactory<Program>()
        .WithWebHostBuilder(web => web.ConfigureServices(services =>
        {
            services.RemoveAll<Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<DraftDb>>();
            services.AddDbContext<DraftDb>(options => options.UseNpgsql(Connection(database)));
        }));
    private static string Connection(string database) => new NpgsqlConnectionStringBuilder(
        Environment.GetEnvironmentVariable("DRAFTSPACE_TEST_CONNECTION")
        ?? "Host=localhost;Username=draftspace;Password=draftspace")
    { Database = database, Pooling = false }.ConnectionString;

    private static async Task Drop(string database)
    {
        await using var connection = new NpgsqlConnection(Connection("postgres"));
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS {database}", connection);
        await command.ExecuteNonQueryAsync();
    }

    private static HttpClient Client(WebApplicationFactory<Program> app, int? user)
    {
        var client = app.CreateClient();
        if (user is not null) client.DefaultRequestHeaders.Add("X-Demo-User", user.ToString());
        return client;
    }
    private static string PathFor(Guid id) => "/api/documents/" + id;
    private static object SaveBody() => new { title = "Release notes", content = JsonSerializer.Deserialize<JsonElement>(RichText) };
    private static async Task<Guid> Create(HttpClient client)
    {
        var response = await client.PostAsync("/api/documents", null);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }
}
