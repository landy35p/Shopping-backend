using Shopping.Api.Services.Abstractions;
using Shopping.Api.Settings;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Shopping.Api.Services.Implementations;

/// <summary>
/// 透過 OpenAI Embeddings API 產生 Embedding 向量。
/// 將 BaseUrl 設為 https://models.inference.ai.azure.com 並填入 GitHub PAT 即可使用 GitHub Models。
/// </summary>
public class OpenAiEmbeddingService(HttpClient httpClient, EmbeddingSettings settings) : IEmbeddingService
{
    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var openAi = settings.OpenAi;

        var requestBody = new
        {
            input = text,
            model = openAi.Model,
            dimensions = 768 // text-embedding-3 系列支援指定回傳維度，以符合資料庫定義的 vector(768)
        };

        var json = JsonSerializer.Serialize(requestBody);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{openAi.BaseUrl}/embeddings")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", openAi.ApiKey);

        using var response = await httpClient.SendAsync(request, ct);
        
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct)
                   ?? throw new InvalidOperationException("Empty response from OpenAI embeddings API");

        // Response format is { "data": [ { "embedding": [ ... ] } ] }
        return responseJson.RootElement
                   .GetProperty("data")[0]
                   .GetProperty("embedding")
                   .EnumerateArray()
                   .Select(e => e.GetSingle())
                   .ToArray();
    }
}
