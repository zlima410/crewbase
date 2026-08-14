using FlooringManager.Application.Common;
using FlooringManager.Application.Estimates;
using FlooringManager.Domain.Estimates;
using FlooringManager.Infrastructure.Persistence;

namespace FlooringManager.Infrastructure.Estimates;

/// <summary>
/// Reconciles an estimate's persisted rooms against the room list a client sent:
/// rooms carrying a known id are updated in place, unknown or absent ids become new
/// rooms, and rooms the client omitted are deleted. Position is reassigned from the
/// request order so the client controls room ordering.
/// </summary>
public sealed class EstimateRoomSynchronizer(ApplicationDbContext db)
{
    public void Synchronize(Estimate estimate, IReadOnlyList<EstimateRoomInput> inputs)
    {
        var existingById = estimate.Rooms.ToDictionary(room => room.Id);
        var incomingIds = inputs
            .Where(input => input.Id.HasValue)
            .Select(input => input.Id!.Value)
            .ToHashSet();

        foreach (var removed in estimate.Rooms.Where(room => !incomingIds.Contains(room.Id)).ToList())
        {
            estimate.Rooms.Remove(removed);
            db.EstimateRooms.Remove(removed);
        }

        var position = 0;
        foreach (var input in inputs)
        {
            var measurement = new RoomMeasurement(input.LengthFeet, input.WidthFeet, input.WastePercentage);

            if (input.Id is Guid id && existingById.TryGetValue(id, out var existing))
            {
                Apply(existing, input, measurement, position++);
            }
            else
            {
                var created = new EstimateRoom { Id = Guid.NewGuid(), EstimateId = estimate.Id };
                Apply(created, input, measurement, position++);
                estimate.Rooms.Add(created);

                db.EstimateRooms.Add(created);
            }
        }
    }

    private static void Apply(
        EstimateRoom room,
        EstimateRoomInput input,
        RoomMeasurement measurement,
        int position)
    {
        room.Name = input.Name.Trim();
        room.LengthFeet = measurement.LengthFeet;
        room.WidthFeet = measurement.WidthFeet;
        room.WastePercentage = measurement.WastePercentage;
        room.SquareFeet = measurement.AreaSquareFeet;
        room.BillableSquareFeet = measurement.BillableSquareFeet;
        room.FlooringType = input.FlooringType;
        room.WorkType = input.WorkType;
        room.InstallationMethod = input.InstallationMethod;
        room.FinishType = input.FinishType;
        room.Notes = OptionalText.Normalize(input.Notes);
        room.LaborRatePerSqFt = input.LaborRatePerSqFt;
        room.MaterialRatePerSqFt = input.MaterialRatePerSqFt;
        room.Position = position;
    }
}
