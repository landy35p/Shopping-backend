using Shopping.Api.Models;

namespace Shopping.Api.Services.Abstractions;

public interface ILlmService
{
    IAsyncEnumerable<string> StreamRecommendationsAsync(
        IReadOnlyList<Product> candidates,
        UserProfile user,
        CancellationToken ct = default);
}
