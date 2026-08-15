using FlooringManager.Domain.Estimates;
using FlooringManager.Domain.Jobs;

namespace FlooringManager.Application.Jobs;

public sealed record JobRoomResponse(
    Guid Id,
    string Name,
    decimal SquareFeet,
    decimal BillableSquareFeet,
    FlooringType FlooringType,
    WorkType WorkType,
    InstallationMethod? InstallationMethod,
    FinishType? FinishType,
    string? Notes,
    int Position);

public sealed record JobResponse(
    Guid Id,
    string JobNumber,
    JobStatus Status,
    Guid EstimateId,
    Guid CustomerId,
    string CustomerName,
    Guid PropertyId,
    string PropertyAddress,
    string? Description,
    string? InternalNotes,
    string? CustomerNotes,
    DateTimeOffset? ScheduledStart,
    DateTimeOffset? ScheduledEnd,
    DateTimeOffset? ActualStart,
    DateTimeOffset? ActualEnd,
    DateTimeOffset CreatedAt,
    IReadOnlyList<JobRoomResponse> Rooms);
