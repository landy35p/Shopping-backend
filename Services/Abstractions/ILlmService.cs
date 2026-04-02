using Shopping.Api.Models;

namespace Shopping.Api.Services.Abstractions;

public interface ILlmService
{
    IAsyncEnumerable<string> StreamRecommendationsAsync(
        IReadOnlyList<Product> candidates,
        UserProfile user,
        CancellationToken ct = default);

    IAsyncEnumerable<string> StreamRecommendationsByPromptAsync(
        IReadOnlyList<Product> candidates,
        string userPrompt,
        CancellationToken ct = default);
}
