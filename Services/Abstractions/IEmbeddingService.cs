namespace Shopping.Api.Services.Abstractions;

public interface IEmbeddingService
{
    Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default);
}
