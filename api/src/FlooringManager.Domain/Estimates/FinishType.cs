namespace FlooringManager.Domain.Estimates;

/// <summary>
/// The surface finish applied to the flooring.
/// </summary>
/// <remarks>
/// Values are explicit and must never be renumbered — they are persisted as ints.
///
/// Deliberately coarse. Per the MVP plan, the product does not model a materials
/// catalog; anything more specific than these categories (a particular stain or
/// product line) belongs in free-text notes.
/// </remarks>
public enum FinishType
{
    WaterBased = 1,
    OilBased = 2,

    /// <summary>Installed raw, to be finished on site.</summary>
    Unfinished = 3,

    /// <summary>Arrives finished from the factory.</summary>
    PreFinished = 4,

    Other = 5
}
