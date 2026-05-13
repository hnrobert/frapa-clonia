# Development Guide

## Development Commands

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the application
dotnet run --project src/FrapaClonia/FrapaClonia.csproj

# Publish for specific platform (NativeAOT enabled by default)
dotnet publish src/FrapaClonia/FrapaClonia.csproj -c Release -r osx-arm64 --self-contained
dotnet publish src/FrapaClonia/FrapaClonia.csproj -c Release -r osx-x64 --self-contained
dotnet publish src/FrapaClonia/FrapaClonia.csproj -c Release -r win-x64 --self-contained
dotnet publish src/FrapaClonia/FrapaClonia.csproj -c Release -r linux-x64 --self-contained

# Publish without NativeAOT (for faster builds during development)
dotnet publish src/FrapaClonia/FrapaClonia.csproj -c Release -r osx-arm64 -p:PublishAot=false
```

## Architecture

The solution follows a layered architecture with clear separation of concerns:

```bash
src/
├── FrapaClonia/              # Entry point, main window, DI setup
├── FrapaClonia.UI/           # Avalonia views, viewmodels, UI services
├── FrapaClonia.Core/         # Service implementations, resources, external integrations
└── FrapaClonia.Shared/       # Shared interfaces, models, utils (no external deps)
```

### Project Dependencies

- **Shared**: No project dependencies, contains pure interfaces, POCO models, and utilities
- **Core**: Depends on Shared, implements all service interfaces. Handles file I/O, process management, TOML serialization (Nett), GitHub API (Octokit), localization
- **UI**: Avalonia views/viewmodels, references Core and Shared
- **Main**: Wires everything together via DI

### Key Services

Registered in `ServiceCollectionExtensions.cs`:

**Core Services (`FrapaClonia.Core/Services/`):**

| Interface | Implementation | Purpose |
|---|---|---|
| `IConfigurationService` | `ConfigurationService` | App settings persistence |
| `IFrpcProcessService` | `FrpcProcessService` | Manages frpc binary execution |
| `IFrpcDownloadService` | `FrpcDownloadService` | Downloads frpc from GitHub releases |
| `IFrpcVersionService` | `FrpcVersionService` | frpc version management |
| `IPresetService` | `PresetService` | Manages frp configuration presets |
| `ITomlSerializer` | `TomlSerializer` | TOML config serialization |
| `ITomlConfigSerializer` | `TomlConfigSerializer` | TOML config serialization for frp configs |
| `ILocalizationService` | `LocalizationService` | i18n support |
| `IValidationService` | `ValidationService` | Input validation |
| `IAutoStartService` | `AutoStartService` | Auto-start management |
| `ISettingsService` | `SettingsService` | Settings persistence |
| `ICacheService` | `CacheService` | Application caching |
| `IDockerDeploymentService` | `DockerDeploymentService` | Docker deployment |
| `INativeDeploymentService` | `NativeDeploymentService` | Native deployment |
| `IPackageManagerService` | `PackageManagerService` | Package management |
| `ISystemServiceManager` | `SystemServiceManager` | System service management (launchd / systemd / Windows Service) |
| `IProcessManager` | `ProcessManager` | Process management |
| `IUpdateService` | `UpdateService` | Self-update: checks GitHub releases, downloads, and applies updates |

**UI Services (`FrapaClonia.UI/Services/`):**

- `NavigationService` — View navigation (viewmodel-first)
- `ThemeService` — Theme management
- `ToastService` — Toast notifications

### Views and ViewModels

Dashboard, ServerConfig, ProxyList, ProxyEditor, VisitorList, VisitorEditor, Deployment, Logs, Settings

Navigation is handled by `NavigationService` with viewmodel-first navigation.

### Configuration Models

Models in `FrapaClonia.Shared/Models/` map to frpc.toml schema:

- `FrpClientConfig` — Root config containing common settings, proxies, visitors
- `ConfigPreset` — Configuration preset with metadata (stored in `presets/{guid}/`)
- `PresetConfig` — Preset configuration wrapper
- `ProxyConfig` — Individual proxy definitions (tcp, http, etc.)
- `AppSettings` — Application settings
- `AppCache` — Application cache data

## NativeAOT

The project uses NativeAOT by default. Key settings in `FrapaClonia.csproj`:

- `PublishAot=true`, `PublishTrimmed=true`, `TrimMode=partial`
- AOT compatibility warnings suppressed for IL2026, IL2070, IL2072, IL2075, IL2067, IL3050
- All projects have `IsAotCompatible=true` and `EnableAotAnalyzer=true`

When adding reflection-dependent code, verify AOT compatibility.

## CI/CD

GitHub Actions workflows in `.github/workflows/`:

- `build.yml` — Builds on main branch pushes for all platforms, uploads artifacts
- `release.yml` — Builds and uploads release assets on GitHub release creation
- `sync-develop.yml` — Syncs develop branch with main

Composite actions in `.github/actions/`:

- `build-and-pack` — Cross-platform compile and package (zip / tar.gz)
- `package-windows-msi` — Builds MSI installer via WiX Toolset v4
- `package-macos-dmg` — Builds DMG disk image with app bundle and /Applications shortcut
- `package-linux-deb` — Builds DEB package for Debian/Ubuntu

Windows artifacts are built with RID `win-x64` and renamed to `windows-x64` for release naming consistency.

## UI Components

Styles are organized in `FrapaClonia.UI/Styles/` with design tokens in `DesignTokens.axaml`.

### Toast Notifications

```csharp
// Constructor injection
public class MyViewModel(ToastService toastService)

// Usage
_toastService?.Success("Saved", "Configuration saved successfully");
_toastService?.Error("Error", "Failed to save configuration");
_toastService?.Warning("Warning", "This action cannot be undone");
_toastService?.Info("Info", "Process completed");
```

### Card Styles

```xml
<Border Classes="card">          <!-- Standard card with border, hover effect -->
<Border Classes="section-card">  <!-- Subtle background, no border -->
<Border Classes="settings-card"> <!-- Card for settings sections -->
<Border Classes="list-card">     <!-- Card for list items, hover highlight -->
<Border Classes="empty-state">   <!-- Centered empty state container -->
```

### Button Styles

```xml
<!-- Variants -->
<Button Classes="primary">      <!-- Filled primary color -->
<Button Classes="secondary">    <!-- Outlined, primary color text -->
<Button Classes="destructive">  <!-- Red, for dangerous actions -->
<Button Classes="ghost">        <!-- Transparent, subtle hover -->

<!-- Layouts -->
<Button Classes="icon-button">       <!-- Square, icon only -->
<Button Classes="icon-text-button">  <!-- Icon + text horizontal -->
```

### Text Styles

| Class | Size | Weight |
|---|---|---|
| `page-title` | 28px | semi-bold |
| `page-subtitle` | 13px | muted |
| `section-title` | 13px | semi-bold |
| `card-title` | 16px | semi-bold |
| `card-description` | 13px | muted, wrapped |
| `field-label` | — | Form field label |
| `hint` | small | muted |

### Page Layout

```xml
<Grid RowDefinitions="Auto,Auto,*">
  <Border Grid.Row="0" Classes="page-header"> ... </Border>
  <Border Grid.Row="1" Classes="page-toolbar"> ... </Border>
  <Border Grid.Row="2" Classes="page-content"> ... </Border>
</Grid>
```

### Form Controls

```xml
<TextBox Classes="field-control" />
<ComboBox Classes="filter-box" />
<ComboBox Classes="settings-dropdown" />
<CheckBox Classes="settings-checkbox" />
```

### Design Tokens

- **Spacing**: `SpacingXS` (4), `SpacingS` (8), `SpacingM` (12), `SpacingL` (16), `SpacingXL` (24)
- **Font Sizes**: `FontSizeS` (11), `FontSizeM` (13), `FontSizeBase` (14), `FontSizeL` (16)
- **Corner Radius**: `CornerRadiusS` (4), `CornerRadiusM` (6), `CornerRadiusL` (8)
- **Content Padding**: `ContentPadding` (24)

## Project-Specific Terms

- `frapa` — Project name prefix
- `clonia` — Project name suffix
- `frpc` — Fast reverse proxy client (the underlying tool this UI wraps)
- `avalonia` — The UI framework
