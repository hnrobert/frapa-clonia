using System.Text.Json.Serialization;

namespace FrapaClonia.UI.ViewModels;

/// <summary>
/// JSON serialization context for Native AOT compatibility (UI layer)
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(AppSettings))]
public partial class AppSettingsContext : JsonSerializerContext
{
}
