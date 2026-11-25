using DraftSpace;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<DraftDb>(options => options.UseNpgsql(
    builder.Configuration.GetConnectionString("DraftSpace")
    ?? "Host=localhost;Database=draftspace;Username=draftspace;Password=draftspace"));
var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
    await scope.ServiceProvider.GetRequiredService<DraftDb>().Database.EnsureCreatedAsync();

app.MapGet("/api/health", () => Results.Ok());
app.Run();
