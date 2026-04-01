namespace Shopping.Api.Models;

public class UserProfile
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Persona { get; set; } = string.Empty;
    public IList<Purchase> Purchases { get; set; } = [];
}
