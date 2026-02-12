using System.Text.Json.Serialization;
using FrapaClonia.Domain.Models;
using FrapaClonia.Infrastructure.Services;

namespace FrapaClonia.Infrastructure;

/// <summary>
/// JSON serialization context for Preset types (Native AOT compatibility)
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ConfigPreset))]
[JsonSerializable(typeof(DeploymentSettings))]
[JsonSerializable(typeof(PresetSettings))]
public partial class PresetSerializationContext : JsonSerializerContext;

/// <summary>
/// JSON serialization context for PresetSettings (Native AOT compatibility)
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(PresetSettings))]
public partial class PresetSettingsContext : JsonSerializerContext;
