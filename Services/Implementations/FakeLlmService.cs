using Shopping.Api.Models;
using Shopping.Api.Services.Abstractions;

namespace Shopping.Api.Services.Implementations;

/// <summary>
/// 開發 / CI 用假實作：回傳固定 mock 推薦說明，每字元延遲 30ms 模擬串流。
/// 不需要 Ollama，可隨時切換使用。
/// </summary>
public class FakeLlmService : ILlmService
{
    private const int CharDelayMs = 30;

    private static readonly string[] MockTemplates =
    [
        "根據您的購買紀錄，這款 {title} 非常符合您的需求。{title} 具備出色的性能與耐用性，是同類商品中的佼佼者。",
        "考量您對 {category} 類商品的偏好，{title} 的高評分（{rating} 星）代表眾多買家的肯定，相信您也會喜歡。",
        "您曾購買的商品顯示您重視品質與功能，{title} 正好滿足這些要求，售價 {price} 元更是物超所值。"
    ];

    public async IAsyncEnumerable<string> StreamRecommendationsAsync(
        IReadOnlyList<Product> candidates,
        UserProfile user,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var (product, index) in candidates.Select((p, i) => (p, i)))
        {
            var template = MockTemplates[index % MockTemplates.Length];
            var text = template
                .Replace("{title}", product.Title)
                .Replace("{category}", product.Category)
                .Replace("{rating}", product.Rating.ToString("F1"))
                .Replace("{price}", product.Price.ToString("F0"));

            foreach (var ch in text)
            {
                ct.ThrowIfCancellationRequested();
                yield return ch.ToString();
                await Task.Delay(CharDelayMs, ct);
            }

            yield return "\n";
        }
    }

    public async IAsyncEnumerable<string> StreamRecommendationsByPromptAsync(
        IReadOnlyList<Product> candidates,
        string userPrompt,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var (product, index) in candidates.Select((p, i) => (p, i)))
        {
            var text = $"根據您的搜尋「{userPrompt}」，推薦 {product.Title}（{product.Rating:F1}星，NT${product.Price:F0}元）。";

            foreach (var ch in text)
            {
                ct.ThrowIfCancellationRequested();
                yield return ch.ToString();
                await Task.Delay(CharDelayMs, ct);
            }

            yield return "\n";
        }
    }
}
