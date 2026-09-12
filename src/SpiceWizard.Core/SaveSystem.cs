using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpiceWizard.Core;

[JsonSourceGenerationOptions(WriteIndented = false, DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(GameState))]
internal partial class SaveContext : JsonSerializerContext { }

/// <summary>JSON round-trip of the whole <see cref="GameState"/>. Source-generated so it survives WASM trimming.</summary>
public static class SaveSystem
{
    public const int Version = 1;

    public static string ToJson(GameState state) =>
        Version + "|" + JsonSerializer.Serialize(state, SaveContext.Default.GameState);

    public static GameState? FromJson(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        int bar = text.IndexOf('|');
        if (bar < 0 || !int.TryParse(text.AsSpan(0, bar), out int version) || version != Version) return null;
        try
        {
            return JsonSerializer.Deserialize(text.AsSpan(bar + 1), SaveContext.Default.GameState);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
