using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using Shopping.Api.Data;
using Shopping.Api.Models;

namespace Shopping.Api.Repositories;

public class ProductRepository(AppDbContext db)
{
    /// <summary>
    /// 以 cosine similarity 搜尋向量最接近的 Top-N 商品（pgvector ivfflat index）。
    /// </summary>
    public async Task<IReadOnlyList<RecommendationResult>> FindSimilarAsync(
        float[] queryEmbedding,
        int topN = 20,
        CancellationToken ct = default)
    {
        var queryVector = new Vector(queryEmbedding);

        return await db.Products
            .Where(p => p.Embedding != null)
            .Select(p => new RecommendationResult
            {
                Product = p,
                SimilarityScore = 1 - (double)p.Embedding!.CosineDistance(queryVector)
            })
            .OrderByDescending(r => r.SimilarityScore)
            .Take(topN)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(
        IEnumerable<string> ids,
        CancellationToken ct = default)
    {
        return await db.Products
            .Where(p => ids.Contains(p.Id))
            .AsNoTracking()
            .ToListAsync(ct);
    }
}
