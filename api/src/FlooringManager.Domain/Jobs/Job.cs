using FlooringManager.Domain.Estimates;
using FlooringManager.Domain.Shared;

namespace FlooringManager.Domain.Jobs;

public sealed class Job : ITenantOwned
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid EstimateId { get; set; }

    public string JobNumber { get; set; } = string.Empty;
    public JobStatus Status { get; set; } = JobStatus.Scheduled;

    public DateTimeOffset? ScheduledStart { get; set; }
    public DateTimeOffset? ScheduledEnd { get; set; }
    public DateTimeOffset? ActualStart { get; set; }
    public DateTimeOffset? ActualEnd { get; set; }

    public string? Description { get; set; }
    public string? InternalNotes { get; set; }
    public string? CustomerNotes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<JobRoom> Rooms { get; set; } = new List<JobRoom>();

    /// <summary>
    /// Snapshots the estimate's agreed scope into a new scheduled job. Does not
    /// mutate the estimate — the caller is responsible for the status transition
    /// in the same transaction as the insert.
    /// </summary>
    public static Job FromAcceptedEstimate(Estimate estimate, string jobNumber, DateTimeOffset createdAt)
    {
        var job = new Job
        {
            Id = Guid.NewGuid(),
            CompanyId = estimate.CompanyId,
            CustomerId = estimate.CustomerId,
            PropertyId = estimate.PropertyId,
            EstimateId = estimate.Id,
            JobNumber = jobNumber,
            Status = JobStatus.Scheduled,
            Description = estimate.Notes,
            CreatedAt = createdAt
        };

        foreach (var room in estimate.Rooms.OrderBy(r => r.Position))
            job.Rooms.Add(JobRoom.CopyFrom(room, job.Id));

        return job;
    }
}
