using FlooringManager.Application.Jobs;
using FlooringManager.Domain.Jobs;

namespace FlooringManager.Infrastructure.Jobs;

public static class JobMapper
{
    public static JobResponse ToResponse(Job job, string customerName, string propertyAddress) =>
        new(
            job.Id,
            job.JobNumber,
            job.Status,
            job.EstimateId,
            job.CustomerId,
            customerName,
            job.PropertyId,
            propertyAddress,
            job.Description,
            job.InternalNotes,
            job.CustomerNotes,
            job.ScheduledStart,
            job.ScheduledEnd,
            job.ActualStart,
            job.ActualEnd,
            job.CreatedAt,
            job.Rooms
                .OrderBy(room => room.Position)
                .Select(ToRoomResponse)
                .ToList());

    private static JobRoomResponse ToRoomResponse(JobRoom room) =>
        new(
            room.Id,
            room.Name,
            room.SquareFeet,
            room.BillableSquareFeet,
            room.FlooringType,
            room.WorkType,
            room.InstallationMethod,
            room.FinishType,
            room.Notes,
            room.Position);
}
