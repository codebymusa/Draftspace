using DraftSpace;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 3 * 1024 * 1024);
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = 300 * 1024);
builder.Services.AddDbContext<DraftDb>(options => options.UseNpgsql(
    builder.Configuration.GetConnectionString("DraftSpace")
    ?? "Host=localhost;Database=draftspace;Username=draftspace;Password=draftspace"));
var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
if (Directory.Exists(app.Environment.WebRootPath))
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

await using (var scope = app.Services.CreateAsyncScope())
    await scope.ServiceProvider.GetRequiredService<DraftDb>().Database.EnsureCreatedAsync();

app.MapGet("/api/health", () => Results.Ok());
app.MapDocuments();
app.Run();

public partial class Program;

