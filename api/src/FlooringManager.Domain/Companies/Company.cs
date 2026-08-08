namespace FlooringManager.Domain.Companies;

public sealed class Company
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Users.User> Users { get; set; } = new List<Users.User>();
}