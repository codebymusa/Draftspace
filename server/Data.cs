using Microsoft.EntityFrameworkCore;

namespace DraftSpace;

public sealed record User(int Id, string Name);

public sealed class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int OwnerId { get; set; }
    public User Owner { get; set; } = null!;
    public string Title { get; set; } = "Untitled document";
    public string Content { get; set; } = """{"type":"doc","content":[{"type":"paragraph"}]}""";
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<DocumentShare> Shares { get; set; } = [];
}

public sealed class DocumentShare
{
    public Guid DocumentId { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}

public sealed class DraftDb(DbContextOptions<DraftDb> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentShare> Shares => Set<DocumentShare>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<User>().HasData(new User(1, "Alex Morgan"), new User(2, "Jordan Lee"), new User(3, "Sam Rivera"));
        model.Entity<Document>().Property(d => d.Title).HasMaxLength(120);
        model.Entity<Document>().Property(d => d.Content).HasColumnType("jsonb");
        model.Entity<DocumentShare>().HasKey(s => new { s.DocumentId, s.UserId });
        model.Entity<Document>().HasMany(d => d.Shares).WithOne().HasForeignKey(s => s.DocumentId);
    }
}
