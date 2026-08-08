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
    [Range(0, 10_000)] decimal MaterialRatePerSqFt);

public sealed record CreateEstimateRequest(
    [Required] Guid CustomerId,
    [Required] Guid PropertyId,
    DateTimeOffset? ExpirationDate,
    [Range(0, double.MaxValue)] decimal Tax,
    [MaxLength(4000)] string? Notes,
    List<EstimateRoomInput> Rooms);

public sealed record UpdateEstimateRequest(
    [Required] Guid CustomerId,
    [Required] Guid PropertyId,
    DateTimeOffset? ExpirationDate,
    [Range(0, double.MaxValue)] decimal Tax,
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