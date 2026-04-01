namespace Shopping.Api.Models;

public class RecommendationResult
{
    public Product Product { get; set; } = null!;
    public double SimilarityScore { get; set; }
}
