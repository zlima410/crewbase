namespace FlooringManager.Infrastructure.Common;

/// <summary>
/// Formats a property address for API responses. Shared so estimate and job
/// payloads cannot drift into different address strings for the same property.
/// </summary>
public static class AddressText
{
    public static string Format(string streetAddress, string city, string state, string postalCode) =>
        $"{streetAddress}, {city}, {state} {postalCode}";
}
