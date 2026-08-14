using System.ComponentModel.DataAnnotations;
using FlooringManager.Domain.Estimates;

namespace FlooringManager.Application.Estimates;

public sealed record EstimateRoomInput(
    Guid? Id,
    [Required, MaxLength(100)] string Name,
    [Range(0.0001, 1000)] decimal LengthFeet,
    [Range(0.0001, 1000)] decimal WidthFeet,
    [Range(0, 100)] decimal WastePercentage,
    FlooringType FlooringType,
    WorkType WorkType,
    [Range(0, 10_000)] decimal LaborRatePerSqFt,
    [Range(0, 10_000)] decimal MaterialRatePerSqFt,
    // Optional, and last so existing callers are unaffected. Omitting them clears
    // them, which is what a room-list replacement should do.
    InstallationMethod? InstallationMethod = null,
    FinishType? FinishType = null,
    [MaxLength(2000)] string? Notes = null);

public sealed record CreateEstimateRequest(
    [Required] Guid CustomerId,
    [Required] Guid PropertyId,
    DateTimeOffset? ExpirationDate,
    // Use typeof(decimal) — [Range(0, 100)] truncates decimals to int and lets 100.01 through.
    [Range(typeof(decimal), "0", "100")] decimal TaxRate,
    [MaxLength(4000)] string? Notes,
    List<EstimateRoomInput> Rooms);

public sealed record UpdateEstimateRequest(
    [Required] Guid CustomerId,
    [Required] Guid PropertyId,
    DateTimeOffset? ExpirationDate,
    [Range(typeof(decimal), "0", "100")] decimal TaxRate,
    [MaxLength(4000)] string? Notes,
    List<EstimateRoomInput> Rooms);

public sealed record EstimateRoomResponse(
    Guid Id,
    string Name,
    decimal LengthFeet,
    decimal WidthFeet,
    decimal WastePercentage,
    decimal SquareFeet,
    decimal BillableSquareFeet,
    FlooringType FlooringType,
    WorkType WorkType,
    InstallationMethod? InstallationMethod,
    FinishType? FinishType,
    string? Notes,
    decimal LaborRatePerSqFt,
    decimal MaterialRatePerSqFt,
    decimal LaborCost,
    decimal MaterialCost,
    decimal RoomTotal,
    int Position);

public sealed record EstimateResponse(
    Guid Id,
    string EstimateNumber,
    EstimateStatus Status,
    Guid CustomerId,
    string CustomerName,
    Guid PropertyId,
    string PropertyAddress,
    DateTimeOffset CreatedDate,
    DateTimeOffset? ExpirationDate,
    DateTimeOffset UpdatedAt,
    decimal LaborSubtotal,
    decimal MaterialSubtotal,
    decimal TaxRate,
    decimal Tax,
    decimal Subtotal,
    decimal Total,
    string? Notes,
    IReadOnlyList<EstimateRoomResponse> Rooms);

public sealed record EstimateListItem(
    Guid Id,
    string EstimateNumber,
    EstimateStatus Status,
    Guid CustomerId,
    string CustomerName,
    string PropertyAddress,
    decimal Total,
    DateTimeOffset CreatedDate);

public sealed record EstimateListResponse(
    IReadOnlyList<EstimateListItem> Items,
    int Page, int PageSize, int Total);