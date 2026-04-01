using Shopping.Api.Services.Abstractions;
using Shopping.Api.Settings;
using System.Net.Http.Json;
using System.Text.Json;

namespace Shopping.Api.Services.Implementations;

/// <summary>
/// 透過 Ollama HTTP API 產生 768 維 Embedding 向量（nomic-embed-text）。
/// </summary>
public class OllamaEmbeddingService(HttpClient httpClient, EmbeddingSettings settings) : IEmbeddingService
{
    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var requestBody = new { model = settings.Ollama.Model, prompt = text };

        using var response = await httpClient.PostAsJsonAsync(
            $"{settings.Ollama.BaseUrl}/api/embeddings", requestBody, ct);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct)
                   ?? throw new InvalidOperationException("Empty response from Ollama embeddings API");

        return json.RootElement
                   .GetProperty("embedding")
                   .EnumerateArray()
                   .Select(e => e.GetSingle())
                   .ToArray();
    }
}
