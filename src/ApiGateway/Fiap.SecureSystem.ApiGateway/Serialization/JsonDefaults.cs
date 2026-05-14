using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fiap.SecureSystem.ApiGateway.Serialization;

internal static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    static JsonDefaults()
    {
        Options.Converters.Add(new JsonStringEnumConverter());
    }
}
