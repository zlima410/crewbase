using FlooringManager.Application.Estimates;
using FlooringManager.Domain.Estimates;

namespace FlooringManager.Infrastructure.Estimates;

/// <summary>
/// Shapes persisted estimates into API responses. Money is recomputed from the
/// stored measurements and rates rather than read from extra columns, so a response
/// can never disagree with the rooms it lists.
/// </summary>
public static class EstimateMapper
{
    public static EstimateResponse ToResponse(
        Estimate estimate,
        string customerName,
        string propertyAddress) =>
        new(
            estimate.Id,
            estimate.EstimateNumber,
            estimate.Status,
            estimate.CustomerId,
            customerName,
            estimate.PropertyId,
            propertyAddress,
            estimate.CreatedDate,
            estimate.ExpirationDate,
            estimate.UpdatedAt,
            estimate.LaborSubtotal,
            estimate.MaterialSubtotal,
            estimate.TaxRate,
            estimate.Tax,
            estimate.LaborSubtotal + estimate.MaterialSubtotal,
            estimate.Total,
            estimate.Notes,
            estimate.Rooms
                .OrderBy(room => room.Position)
                .Select(ToRoomResponse)
                .ToList());

    private static EstimateRoomResponse ToRoomResponse(EstimateRoom room)
    {
        var pricing = new RoomPricing(room.BillableSquareFeet, room.LaborRatePerSqFt, room.MaterialRatePerSqFt);

        return new EstimateRoomResponse(
            room.Id,
            room.Name,
            room.LengthFeet,
            room.WidthFeet,
            room.WastePercentage,
            room.SquareFeet,
            room.BillableSquareFeet,
            room.FlooringType,
            room.WorkType,
            room.InstallationMethod,
            room.FinishType,
            room.Notes,
            room.LaborRatePerSqFt,
            room.MaterialRatePerSqFt,
            pricing.LaborCost,
            pricing.MaterialCost,
            pricing.RoomTotal,
            room.Position);
    }

    public static string FormatAddress(string streetAddress, string city, string state, string postalCode) =>
        $"{streetAddress}, {city}, {state} {postalCode}";
}
