namespace Shopping.Api.Models;

public class Purchase
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public DateTime PurchasedAt { get; set; }

    public UserProfile? User { get; set; }
    public Product? Product { get; set; }
}
