using Microsoft.EntityFrameworkCore;
using Shopping.Api.Data;
using Shopping.Api.Models;
using Shopping.Api.Repositories;
using Shopping.Api.Services.Abstractions;

namespace Shopping.Api.Services.Implementations;

/// <summary>
/// 兩階段推薦 Pipeline：
///   Stage 1 — IEmbeddingService + pgvector 向量召回 Top-20（< 50ms）
///   Stage 2 — ILlmService 串流生成個性化說明（1–3s SSE）
/// </summary>
public class RecommendationService(
    IEmbeddingService embeddingService,
    ILlmService llmService,
    ProductRepository productRepository,
    AppDbContext db)
{
    public async IAsyncEnumerable<(string EventType, object Data)> StreamAsync(
        string userId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var user = await db.Users
            .Include(u => u.Purchases)
            .ThenInclude(p => p.Product)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new KeyNotFoundException($"User '{userId}' not found");

        // Stage 1: 向量召回
        var purchasedText = string.Join(" ", user.Purchases
            .Where(p => p.Product != null)
            .Select(p => $"{p.Product!.Title} {p.Product.Category}"));

        var queryEmbedding = await embeddingService.GetEmbeddingAsync(
            string.IsNullOrWhiteSpace(purchasedText) ? user.Persona : purchasedText, ct);

        var candidates = (await productRepository.FindSimilarAsync(queryEmbedding, topN: 5, ct))
            .Select(r => r.Product)
            .ToList();

        // 先送出商品卡片（event: product）
        foreach (var product in candidates)
        {
            yield return ("product", new
            {
                product.Id,
                product.Title,
                product.Price,
                product.Rating,
                product.ImageUrl
            });
        }

        // Stage 2: LLM 串流推薦說明（event: reasoning）
        await foreach (var chunk in llmService.StreamRecommendationsAsync(candidates, user, ct))
        {
            yield return ("reasoning", new { text = chunk });
        }

        yield return ("done", new { });
    }
}
