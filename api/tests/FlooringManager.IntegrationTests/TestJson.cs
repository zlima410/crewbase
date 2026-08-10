using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlooringManager.IntegrationTests;

/// <summary>
/// JSON options matching the API's JsonStringEnumConverter so test clients
/// can deserialize responses that emit enums as strings.
/// </summary>
public static class TestJson
{
    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
