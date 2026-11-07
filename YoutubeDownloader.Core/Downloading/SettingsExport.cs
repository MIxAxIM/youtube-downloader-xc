using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YoutubeDownloader.Core.Downloading;

public static class SettingsExport
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string ToJson(IDictionary<string, object?> values)
    {
        values.Remove("ProxyPassword");
        values.Remove("LastAuthCookies");
        return JsonSerializer.Serialize(values, Options);
    }

    public static Dictionary<string, JsonElement> FromJson(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, Options) ?? [];
}
