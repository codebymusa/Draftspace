using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace DraftSpace;

public static class Documents
{
    public static void MapDocuments(this WebApplication app)
    {
        app.MapGet("/api/users", (DraftDb db) => db.Users.OrderBy(u => u.Id).ToListAsync());

        var routes = app.MapGroup("/api/documents");
        routes.AddEndpointFilter(async (context, next) =>
        {
            var http = context.HttpContext;
            var db = http.RequestServices.GetRequiredService<DraftDb>();
            if (!int.TryParse(http.Request.Headers["X-Demo-User"], out var id) ||
                !await db.Users.AnyAsync(u => u.Id == id))
                return Error(401, "Choose a valid demo user.");
            http.Items["DemoUser"] = id;
            return await next(context);
        });

        routes.MapGet("", async (HttpContext http, DraftDb db) =>
        {
            var user = Actor(http);
            return await Accessible(db, user).OrderByDescending(d => d.UpdatedAt)
                .Select(d => new { d.Id, d.Title, d.UpdatedAt, d.Owner, IsOwner = d.OwnerId == user })
                .ToListAsync();
        });

        routes.MapPost("", async (HttpContext http, DraftDb db) =>
        {
            var user = await db.Users.SingleAsync(u => u.Id == Actor(http));
            return await Create(db, new Document { Owner = user });
        });

        routes.MapGet("/{id:guid}", async (Guid id, HttpContext http, DraftDb db) =>
        {
            var document = await Find(db, id, Actor(http));
            return document is null ? Missing() : Results.Ok(View(document, Actor(http)));
        });

        routes.MapPut("/{id:guid}", async (Guid id, SaveDocument input, HttpContext http, DraftDb db) =>
        {
            var document = await Find(db, id, Actor(http));
            if (document is null) return Missing();
            var title = input.Title?.Trim();
            if (string.IsNullOrEmpty(title) || title.Length > 120 || title.Contains('\0'))
                return Error(400, "Enter a title of 1 to 120 characters.");
            if (!ValidContent(input.Content))
                return Error(400, "Provide valid TipTap document JSON, up to 2 MB.");
            document.Title = title;
            document.Content = input.Content.GetRawText();
            document.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(View(document, Actor(http)));
        });

        routes.MapGet("/{id:guid}/shares", async (Guid id, HttpContext http, DraftDb db) =>
        {
            var document = await Find(db, id, Actor(http));
            if (document is null) return Missing();
            if (document.OwnerId != Actor(http)) return Error(403, "Only the owner can manage sharing.");
            return Results.Ok(await db.Shares.Where(s => s.DocumentId == id).Select(s => s.User)
                .OrderBy(u => u.Name).ToListAsync());
        });

        routes.MapPut("/{id:guid}/shares/{userId:int}", async (Guid id, int userId, HttpContext http, DraftDb db) =>
        {
            var document = await Find(db, id, Actor(http));
            if (document is null) return Missing();
            if (document.OwnerId != Actor(http)) return Error(403, "Only the owner can manage sharing.");
            if (userId == document.OwnerId || !await db.Users.AnyAsync(u => u.Id == userId))
                return Error(400, "Choose another demo user.");
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""INSERT INTO "Shares" ("DocumentId", "UserId") VALUES ({id}, {userId}) ON CONFLICT DO NOTHING""");
            return Results.NoContent();
        });

        routes.MapDelete("/{id:guid}/shares/{userId:int}", async (Guid id, int userId, HttpContext http, DraftDb db) =>
        {
            var document = await Find(db, id, Actor(http));
            if (document is null) return Missing();
            if (document.OwnerId != Actor(http)) return Error(403, "Only the owner can manage sharing.");
            await db.Shares.Where(s => s.DocumentId == id && s.UserId == userId).ExecuteDeleteAsync();
            return Results.NoContent();
        });

        routes.MapPost("/import", Import);
    }

    private static async Task<IResult> Import(HttpContext http, DraftDb db)
    {
        if (!http.Request.HasFormContentType) return Error(400, "Upload a .txt file.");
        IFormFile? file;
        try { file = (await http.Request.ReadFormAsync()).Files.GetFile("file"); }
        catch (InvalidDataException) { return Error(400, "The upload failed. Choose a .txt file up to 256 KB."); }
        if (file is null || !Path.GetExtension(file.FileName).Equals(".txt", StringComparison.OrdinalIgnoreCase))
            return Error(400, "Choose a .txt file.");
        if (file.Length is 0 or > 256 * 1024)
            return Error(400, "The file must be between 1 byte and 256 KB.");
        using var bytes = new MemoryStream();
        await file.CopyToAsync(bytes);
        string text;
        try { text = new System.Text.UTF8Encoding(false, true).GetString(bytes.ToArray()); }
        catch (System.Text.DecoderFallbackException) { return Error(400, "The file must contain valid UTF-8 text."); }
        text = text.TrimStart('\uFEFF');
        if (text.Any(c => char.IsControl(c) && c is not ('\r' or '\n' or '\t')))
            return Error(400, "The file contains binary or unsupported control characters.");
        var content = JsonSerializer.Serialize(new
        {
            type = "doc",
            content = text.ReplaceLineEndings("\n").Split("\n\n").Select(paragraph => new
            {
                type = "paragraph",
                content = paragraph.Length == 0 ? [] : new[] { new { type = "text", text = paragraph } }
            })
        });
        if (!ValidContent(JsonSerializer.Deserialize<JsonElement>(content)))
            return Error(400, "The imported document exceeds the editor size limit.");
        var title = Path.GetFileNameWithoutExtension(file.FileName.Replace('\\', '/')).Trim();
        var user = await db.Users.SingleAsync(u => u.Id == Actor(http));
        return await Create(db, new Document
        {
            Owner = user, Content = content,
            Title = string.IsNullOrWhiteSpace(title) || title.Contains('\0') ? "Imported document" : title[..Math.Min(120, title.Length)]
        });
    }

    private static bool ValidContent(JsonElement content) =>
        content.ValueKind == JsonValueKind.Object &&
        content.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String &&
        type.GetString() == "doc" && content.TryGetProperty("content", out var nodes) &&
        nodes.ValueKind == JsonValueKind.Array && nodes.GetArrayLength() > 0 &&
        System.Text.Encoding.UTF8.GetByteCount(content.GetRawText()) <= 2 * 1024 * 1024 &&
        nodes.EnumerateArray().All(node => ValidNode(node, "doc", 0));

    private static bool ValidNode(JsonElement node, string parent, int depth)
    {
        if (depth > 20 || node.ValueKind != JsonValueKind.Object ||
            !node.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String) return false;
        var name = type.GetString()!;
        var allowed = parent switch
        {
            "paragraph" or "heading" => name is "text" or "hardBreak",
            "bulletList" or "orderedList" => name == "listItem",
            _ => name is "paragraph" or "heading" or "bulletList" or "orderedList"
        };
        if (!allowed) return false;
        if (name == "text" && (!node.TryGetProperty("text", out var text) ||
            text.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(text.GetString()) ||
            text.GetString()!.Contains('\0'))) return false;
        if (node.TryGetProperty("marks", out var marks) &&
            (marks.ValueKind != JsonValueKind.Array || !marks.EnumerateArray().All(mark =>
                mark.ValueKind == JsonValueKind.Object && mark.TryGetProperty("type", out var key) &&
                key.ValueKind == JsonValueKind.String && key.GetString() is "bold" or "italic" or "underline")))
            return false;
        if (name is "text" or "hardBreak") return !node.TryGetProperty("content", out _);
        if (!node.TryGetProperty("content", out var children)) return name is "paragraph" or "heading";
        return children.ValueKind == JsonValueKind.Array &&
            (name is "paragraph" or "heading" || children.GetArrayLength() > 0) &&
            (name != "listItem" || (children[0].ValueKind == JsonValueKind.Object &&
                children[0].TryGetProperty("type", out var first) && first.ValueKind == JsonValueKind.String &&
                first.GetString() == "paragraph")) &&
            children.EnumerateArray().All(child => ValidNode(child, name, depth + 1));
    }

    private static int Actor(HttpContext http) => (int)http.Items["DemoUser"]!;
    private static IQueryable<Document> Accessible(DraftDb db, int user) =>
        db.Documents.Where(d => d.OwnerId == user || d.Shares.Any(s => s.UserId == user));
    private static Task<Document?> Find(DraftDb db, Guid id, int user) =>
        Accessible(db, user).Include(d => d.Owner).SingleOrDefaultAsync(d => d.Id == id);
    private static IResult Error(int status, string detail) => Results.Problem(detail, statusCode: status);
    private static IResult Missing() => Error(404, "This document is unavailable for your demo user.");
    private static object View(Document d, int user) => new
    {
        d.Id, d.Title, d.UpdatedAt, d.Owner, IsOwner = d.OwnerId == user,
        Content = JsonSerializer.Deserialize<JsonElement>(d.Content)
    };
    private static async Task<IResult> Create(DraftDb db, Document document)
    {
        db.Documents.Add(document);
        await db.SaveChangesAsync();
        return Results.Created($"/api/documents/{document.Id}", View(document, document.OwnerId));
    }
}

public sealed record SaveDocument(string? Title, JsonElement Content);
