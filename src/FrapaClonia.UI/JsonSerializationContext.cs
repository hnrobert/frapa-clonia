using System.Text.Json.Serialization;
using FrapaClonia.UI.ViewModels;

namespace FrapaClonia.UI;

/// <summary>
/// JSON serialization context for Native AOT compatibility (UI layer)
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(AppSettings))]
public partial class AppSettingsContext : JsonSerializerContext;
