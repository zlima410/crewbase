namespace FlooringManager.Application.Common;

/// <summary>
/// Normalization for optional free-text input.
/// </summary>
public static class OptionalText
{
    /// <summary>
    /// Trims surrounding whitespace and collapses blank input to null, so a cleared
    /// form field and an omitted one are stored identically. Without this, a user who
    /// deletes their notes leaves an empty string behind, which reads as "present but
    /// empty" everywhere downstream.
    /// </summary>
    public static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
