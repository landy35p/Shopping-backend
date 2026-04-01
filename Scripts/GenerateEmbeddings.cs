using Shopping.Api.Data;
using Shopping.Api.Services.Abstractions;

namespace Shopping.Api.Scripts;

/// <summary>
/// Generates embedding vectors for all products that don't have one yet,
/// then writes them back to the database.
///
/// Usage:  dotnet run -- embed
///
/// By default uses whatever EmbeddingSettings.Provider is configured
/// (default: "fake"). Switch to "ollama" in appsettings.json for real vectors.
/// </summary>
public static class GenerateEmbeddings
{
    private const int BatchSize = 20;

    public static async Task RunAsync(IServiceProvider services)
    {
        Console.WriteLine("=== GenerateEmbeddings: Starting ===");

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();

        var products = db.Products
            .Where(p => p.Embedding == null)
            .ToList();

        Console.WriteLine($"Products without embeddings: {products.Count}");

        if (products.Count == 0)
        {
            Console.WriteLine("All products already have embeddings. Nothing to do.");
            return;
        }

        int done = 0;

        foreach (var batch in products.Chunk(BatchSize))
        {
            foreach (var product in batch)
            {
                var inputText = $"{product.Title}. {product.Description}. Category: {product.Category}";
                var vector = await embeddingService.GetEmbeddingAsync(inputText);
                product.Embedding = new Pgvector.Vector(vector);
                done++;
            }

            await db.SaveChangesAsync();

            Console.WriteLine(
                $"  [{done}/{products.Count}] last: {batch[^1].Title[..Math.Min(60, batch[^1].Title.Length)]}...");
        }

        Console.WriteLine($"\n=== GenerateEmbeddings: Done ✅ ===");
        Console.WriteLine($"  Embedded: {done} products");
    }
}
