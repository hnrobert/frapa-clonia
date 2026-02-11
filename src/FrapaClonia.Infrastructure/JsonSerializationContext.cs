using System.Text.Json.Serialization;
using FrapaClonia.Core.Interfaces;

namespace FrapaClonia.Infrastructure;

/// <summary>
/// JSON serialization context for Profile types (Native AOT compatibility)
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Profile))]
[JsonSerializable(typeof(ProfileInfo))]
[JsonSerializable(typeof(Dictionary<string, string>))]
public partial class ProfileContext : JsonSerializerContext;
