using Shopping.Api.Services.Abstractions;

namespace Shopping.Api.Services.Implementations;

/// <summary>
/// 開發 / CI 用假 Embedding：回傳固定種子向量（768 維），查詢結果可重現。
/// </summary>
public class FakeEmbeddingService : IEmbeddingService
{
    private const int Dimensions = 768;

    public Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var seed = text.GetHashCode();
        var rng = new Random(seed);
        var vector = Enumerable.Range(0, Dimensions)
                               .Select(_ => (float)rng.NextDouble())
                               .ToArray();
        return Task.FromResult(vector);
    }
}
