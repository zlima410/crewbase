using FlooringManager.Domain.Shared;

namespace FlooringManager.Domain.Customers;

public sealed class Customer : ITenantOwned
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Properties.Property> Properties { get; set; } = new List<Properties.Property>();
}