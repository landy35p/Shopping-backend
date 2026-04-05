using Shopping.Api.Models;
using Shopping.Api.Services.Abstractions;
using Shopping.Api.Settings;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Shopping.Api.Services.Implementations;

/// <summary>
/// 透過 OpenAI Chat Completions streaming API 呼叫雲端 LLM。
/// 將 BaseUrl 設為 https://models.inference.ai.azure.com 並填入 GitHub PAT 即可使用 GitHub Models 免費方案。
/// </summary>
public class OpenAiLlmService(HttpClient httpClient, LlmSettings settings) : ILlmService
{
    public async IAsyncEnumerable<string> StreamRecommendationsAsync(
        IReadOnlyList<Product> candidates,
        UserProfile user,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var prompt = BuildPersonaPrompt(candidates, user);
        await foreach (var token in StreamAsync(prompt, ct))
            yield return token;
    }

    public async IAsyncEnumerable<string> StreamRecommendationsByPromptAsync(
        IReadOnlyList<Product> candidates,
        string userPrompt,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var prompt = BuildSearchPrompt(candidates, userPrompt);
        await foreach (var token in StreamAsync(prompt, ct))
            yield return token;
    }

    // ── Private helpers ────────────────────────────────────────────────

    private string BuildPersonaPrompt(IReadOnlyList<Product> candidates, UserProfile user)
    {
        var productList = string.Join("\n", candidates.Select((p, i) =>
            $"{i + 1}. {p.Title}（{p.Category}，{p.Rating:F1}星，NT${p.Price:F0}）"));

        return
            $"""
             你是一位友善的購物助手，請根據用戶的購買紀錄，逐一說明以下 {candidates.Count} 件推薦商品的推薦理由。
             用戶名稱：{user.Name}（{user.Persona}）
             推薦商品：
             {productList}
             請用繁體中文，每件商品 1-2 句，語氣自然親切。
             """;
    }

    private string BuildSearchPrompt(IReadOnlyList<Product> candidates, string userPrompt)
    {
        var productList = string.Join("\n", candidates.Select((p, i) =>
            $"{i + 1}. {p.Title}（{p.Category}，{p.Rating:F1}星，NT${p.Price:F0}）"));

        return
            $"""
             你是一位友善的購物助手，使用者正在搜尋：「{userPrompt}」
             請根據此搜尋意圖，逐一說明以下 {candidates.Count} 件推薦商品的推薦理由。
             推薦商品：
             {productList}
             請用繁體中文，每件商品 1-2 句，語氣自然親切。
             """;
    }

    private async IAsyncEnumerable<string> StreamAsync(
        string prompt,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var openAi = settings.OpenAi;

        var requestBody = new
        {
            model = openAi.Model,
            messages = new[] { new { role = "user", content = prompt } },
            stream = true
        };

        var json = JsonSerializer.Serialize(requestBody);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{openAi.BaseUrl}/chat/completions")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", openAi.ApiKey);

        using var response = await httpClient.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            if (!line.StartsWith("data: ")) continue;

            var data = line["data: ".Length..];
            if (data == "[DONE]") yield break;

            JsonDocument doc;
            try { doc = JsonDocument.Parse(data); }
            catch (JsonException) { continue; }

            using (doc)
            {
                if (!doc.RootElement.TryGetProperty("choices", out var choices)) continue;
                if (choices.GetArrayLength() == 0) continue;
                var delta = choices[0].GetProperty("delta");
                if (!delta.TryGetProperty("content", out var content)) continue;

                var text = content.GetString();
                if (!string.IsNullOrEmpty(text))
                    yield return text;
            }
        }
    }
}
