using System.ComponentModel.DataAnnotations;

namespace FlooringManager.Application.Properties;

public sealed record CreatePropertyRequest(
    [Required, MaxLength(300)] string StreetAddress,
    [Required, MaxLength(100)] string City,
    [Required, MaxLength(50)] string State,
    [Required, MaxLength(20)] string PostalCode,
    [MaxLength(2000)] string? AccessNotes);
public sealed record UpdatePropertyRequest(
    [Required, MaxLength(300)] string StreetAddress,
    [Required, MaxLength(100)] string City,
    [Required, MaxLength(50)] string State,
    [Required, MaxLength(20)] string PostalCode,
    [MaxLength(2000)] string? AccessNotes);
public sealed record PropertyResponse(
    Guid Id,
    Guid CustomerId,
    string StreetAddress,
    string City,
    string State,
    string PostalCode,
    string? AccessNotes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);