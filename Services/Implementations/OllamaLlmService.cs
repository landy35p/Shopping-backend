using Shopping.Api.Services.Abstractions;
using Shopping.Api.Settings;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Shopping.Api.Models;

namespace Shopping.Api.Services.Implementations;

/// <summary>
/// 透過 Ollama HTTP API 串流呼叫本地 LLM（qwen2.5:7b）。
/// </summary>
public class OllamaLlmService(HttpClient httpClient, LlmSettings settings) : ILlmService
{
    public async IAsyncEnumerable<string> StreamRecommendationsAsync(
        IReadOnlyList<Product> candidates,
        UserProfile user,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var productList = string.Join("\n", candidates.Select((p, i) =>
            $"{i + 1}. {p.Title}（{p.Category}，{p.Rating:F1}星，NT${p.Price:F0}）"));

        var prompt =
            $"""
             你是一位友善的購物助手，請根據用戶的購買紀錄，逐一說明以下 {candidates.Count} 件推薦商品的推薦理由。
             用戶名稱：{user.Name}（{user.Persona}）
             推薦商品：
             {productList}
             請用繁體中文，每件商品 1-2 句，語氣自然親切。
             """;

        var requestBody = new
        {
            model = settings.Ollama.Model,
            messages = new[] { new { role = "user", content = prompt } },
            stream = true
        };

        using var response = await httpClient.PostAsJsonAsync(
            $"{settings.Ollama.BaseUrl}/api/chat", requestBody, ct);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new System.IO.StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var json = JsonDocument.Parse(line);
            var content = json.RootElement
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (!string.IsNullOrEmpty(content))
                yield return content;
        }
    }

    public async IAsyncEnumerable<string> StreamRecommendationsByPromptAsync(
        IReadOnlyList<Product> candidates,
        string userPrompt,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var productList = string.Join("\n", candidates.Select((p, i) =>
            $"{i + 1}. {p.Title}（{p.Category}，{p.Rating:F1}星，NT${p.Price:F0}）"));

        var prompt =
            $"""
             你是一位友善的購物助手，使用者正在搜尋：「{userPrompt}」
             請根據此搜尋意圖，逐一說明以下 {candidates.Count} 件推薦商品的推薦理由。
             推薦商品：
             {productList}
             請用繁體中文，每件商品 1-2 句，語氣自然親切。
             """;

        var requestBody = new
        {
            model = settings.Ollama.Model,
            messages = new[] { new { role = "user", content = prompt } },
            stream = true
        };

        using var response = await httpClient.PostAsJsonAsync(
            $"{settings.Ollama.BaseUrl}/api/chat", requestBody, ct);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new System.IO.StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var json = JsonDocument.Parse(line);
            var content = json.RootElement
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (!string.IsNullOrEmpty(content))
                yield return content;
        }
    }
}
