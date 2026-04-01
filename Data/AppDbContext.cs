using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using Shopping.Api.Models;

namespace Shopping.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<UserProfile> Users => Set<UserProfile>();
    public DbSet<Purchase> Purchases => Set<Purchase>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Embedding).HasColumnType("vector(768)");
            entity.HasIndex(p => p.Embedding)
                  .HasMethod("ivfflat")
                  .HasOperators("vector_cosine_ops")
                  .HasStorageParameter("lists", 100);
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasMany(u => u.Purchases)
                  .WithOne(p => p.User)
                  .HasForeignKey(p => p.UserId);
        });

        modelBuilder.Entity<Purchase>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasOne(p => p.Product)
                  .WithMany()
                  .HasForeignKey(p => p.ProductId);
        });
    }
}
