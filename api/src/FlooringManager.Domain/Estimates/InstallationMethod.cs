namespace FlooringManager.Domain.Estimates;

/// <summary>
/// How the flooring is (or was) fastened to the subfloor.
/// </summary>
/// <remarks>
/// Values are explicit and must never be renumbered — they are persisted as ints.
///
/// The property is nullable, so "not recorded" is <c>null</c>. <see cref="Unknown"/>
/// is a different answer: the crew looked at an existing floor and could not tell
/// how it was installed. That distinction matters on refinishing work, where the
/// method drives how the floor can be sanded.
/// </remarks>
public enum InstallationMethod
{
    NailDown = 1,
    GlueDown = 2,
    Floating = 3,

    /// <summary>Already installed; this job is not laying new flooring.</summary>
    Existing = 4,

    /// <summary>Inspected but not identifiable.</summary>
    Unknown = 5
}
