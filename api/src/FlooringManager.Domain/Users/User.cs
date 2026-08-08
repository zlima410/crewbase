using FlooringManager.Domain.Companies;
using FlooringManager.Domain.Shared;

namespace FlooringManager.Domain.Users;

public sealed class User
{
    public Guid Id { get; set; }

    public Guid AuthProviderUserId { get; set; }

    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
}