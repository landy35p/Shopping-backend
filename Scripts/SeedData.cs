using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shopping.Api.Data;
using Shopping.Api.Models;

namespace Shopping.Api.Scripts;

/// <summary>
/// Seeds the database with 200 Electronics products from products.json
/// plus 3 mock users with purchase history.
///
/// Usage:  dotnet run -- seed
/// </summary>
public static class SeedData
{
    private sealed record ProductJson(
        string Id,
        string Title,
        string Description,
        decimal Price,
        float Rating,
        string Category,
        string ImageUrl);

    public static async Task RunAsync(IServiceProvider services)
    {
        Console.WriteLine("=== SeedData: Starting ===");

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Apply any pending migrations
        Console.WriteLine("Ensuring database is up-to-date...");
        await db.Database.MigrateAsync();

        // Locate products.json (Scripts/ relative to working directory)
        var scriptsDir = Path.Combine(Directory.GetCurrentDirectory(), "Scripts");
        var jsonPath = Path.Combine(scriptsDir, "products.json");

        if (!File.Exists(jsonPath))
        {
            Console.Error.WriteLine(
                $"ERROR: {jsonPath} not found.\n" +
                "Run 'python Scripts/download-dataset.py' first.");
            return;
        }

        // Clear existing data in reverse dependency order
        Console.WriteLine("Clearing existing data...");
        await db.Purchases.ExecuteDeleteAsync();
        await db.Products.ExecuteDeleteAsync();
        await db.Users.ExecuteDeleteAsync();

        // ── Products ──────────────────────────────────────────────────
        var json = await File.ReadAllTextAsync(jsonPath);
        var productJsonList = JsonSerializer.Deserialize<List<ProductJson>>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        var products = productJsonList
            .Select(p => new Product
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                Price = p.Price,
                Rating = p.Rating,
                Category = p.Category,
                ImageUrl = p.ImageUrl,
                Embedding = null
            })
            .ToList();

        Console.WriteLine($"Inserting {products.Count} products...");
        await db.Products.AddRangeAsync(products);
        await db.SaveChangesAsync();

        // ── Users (3 personas) ────────────────────────────────────────
        var users = new[]
        {
            new UserProfile { Id = "user-tech",  Name = "科技達人 Alex",  Persona = "tech-enthusiast" },
            new UserProfile { Id = "user-home",  Name = "居家主義 Lisa",  Persona = "home-lifestyle" },
            new UserProfile { Id = "user-sport", Name = "運動愛好者 Ken", Persona = "sports-active" },
        };

        Console.WriteLine("Inserting 3 mock users...");
        await db.Users.AddRangeAsync(users);
        await db.SaveChangesAsync();

        // ── Purchases (persona-aligned product pools) ─────────────────
        var rng = new Random(42);

        var techPool = products.Where(p => p.Category is
            "Headphones" or "Earbuds" or "Laptops" or "Gaming Laptops"
            or "Gaming Peripherals" or "Monitors" or "Keyboards" or "Mice").ToList();

        var homePool = products.Where(p => p.Category is
            "Smart Home" or "Smart Appliances" or "Speakers"
            or "Streaming" or "Security" or "Networking").ToList();

        var sportPool = products.Where(p => p.Category is
            "Cameras" or "Accessories" or "Storage"
            or "Chargers" or "Phone Cases").ToList();

        Console.WriteLine("Creating purchase history...");
        await SeedPurchasesAsync(db, rng, users[0], techPool.Count > 0 ? techPool : products, 5);
        await SeedPurchasesAsync(db, rng, users[1], homePool.Count > 0 ? homePool : products, 4);
        await SeedPurchasesAsync(db, rng, users[2], sportPool.Count > 0 ? sportPool : products, 4);

        Console.WriteLine("=== SeedData: Done ✅ ===");
        Console.WriteLine($"  Products : {products.Count}");
        Console.WriteLine($"  Users    : {users.Length}");
        Console.WriteLine($"  Purchases: {5 + 4 + 4}");
    }

    private static async Task SeedPurchasesAsync(
        AppDbContext db,
        Random rng,
        UserProfile user,
        List<Product> pool,
        int count)
    {
        var picked = pool.OrderBy(_ => rng.Next()).Take(count);
        foreach (var product in picked)
        {
            db.Purchases.Add(new Purchase
            {
                Id = Guid.NewGuid().ToString(),
                UserId = user.Id,
                ProductId = product.Id,
                PurchasedAt = DateTime.UtcNow.AddDays(-rng.Next(1, 90))
            });
        }
        await db.SaveChangesAsync();
    }
}
