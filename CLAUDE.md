# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) working with code in this repository.

## Project Overview

**frapa-clonia** is a cross-platform desktop client for frpc (fast reverse proxy client) built with Avalonia UI and .NET 10. It provides a GUI for managing frp client configurations, proxies, visitors, and deployments.

### Technology Stack

- **Framework**: Avalonia UI 11.3 (cross-platform desktop UI framework)
- **Language**: C# / .NET 10
- **MVVM**: CommunityToolkit.Mvvm
- **DI**: Microsoft.Extensions.DependencyInjection
- **Logging**: Serilog
- **Icons**: Material.Icons.Avalonia
- **License**: Apache License 2.0

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
├── FrapaClonia.Core/         # Business interfaces, no infrastructure deps
├── FrapaClonia.Infrastructure/# Service implementations, external integrations
└── FrapaClonia.Domain/       # Pure data models (FrpClientConfig, ProxyConfig, etc.)
```

### Project Dependencies

- **Domain**: No dependencies (pure POCO models)
- **Core**: Depends on Domain, defines service interfaces
- **Infrastructure**: Implements Core interfaces, handles file I/O, process management, TOML serialization (Nett), GitHub API (Octokit)
- **UI**: Avalonia views/viewmodels, references Core, Infrastructure, Domain
- **Main**: Wires everything together via DI

### Key Services

Registered in `ServiceCollectionExtensions.cs`:

- `IConfigurationService` / `ConfigurationService` - App settings persistence
- `IFrpcProcessService` / `FrpcProcessService` - Manages frpc binary execution
- `IFrpcDownloader` / `FrpcDownloader` - Downloads frpc from GitHub releases
- `IProfileService` / `ProfileService` - Manages frp configuration profiles
- `ITomlSerializer` / `TomlSerializer` - TOML config serialization
- `INavigationService` / `NavigationService` - View navigation
- `ILocalizationService` / `LocalizationService` - i18n support

### Views and ViewModels

Located in `FrapaClonia.UI/`:

- Dashboard, ServerConfig, ProxyList, ProxyEditor, VisitorList, VisitorEditor, Deployment, Logs, Settings

Navigation is handled by `NavigationService` with viewmodel-first navigation.

### Styling

- Design tokens defined in `Styles/DesignTokens.axaml` (spacing, colors, typography, etc.)
- Shared styles in `Styles/SharedStyles.axaml`
- Use `ResourceInitializer` for runtime resource additions (e.g., localized strings)

## NativeAOT Considerations

The project uses NativeAOT by default for smaller binary sizes. Key settings in `FrapaClonia.csproj`:

- `PublishAot=true`, `PublishTrimmed=true`, `TrimMode=partial`
- AOT compatibility warnings suppressed for IL2026, IL2070, IL2072, IL2075, IL2067, IL3050
- All projects have `IsAotCompatible=true` and `EnableAotAnalyzer=true`

When adding reflection-dependent code, verify AOT compatibility.

## Configuration Models

Domain models in `FrapaClonia.Domain/Models/` map to frpc.toml schema:

- `FrpClientConfig` - Root config containing common settings, proxies, visitors
- `ClientCommonConfig` - Server connection, auth, transport settings
- `ProxyConfig` - Individual proxy definitions (tcp, http, etc.)
- `VisitorConfig` - STCP/XTCP/SUDP visitor definitions

## CI/CD

GitHub Actions workflows in `.github/workflows/`:

- `build.yml` - Builds on main branch pushes for all platforms
- `release.yml` - Publishes artifacts on GitHub release

Build uses composite action `.github/actions/build-and-pack/` which handles cross-platform compilation and packaging.

## UI Components

Styles are organized in `FrapaClonia.UI/Styles/` with design tokens in `DesignTokens.axaml`.

### Toast Notifications

Toast notifications are managed by `ToastService` (registered in DI). Inject it into ViewModels:

```csharp
// Constructor injection
public class MyViewModel(ToastService toastService)

// Usage
_toastService?.Success("Saved", "Configuration saved successfully");
_toastService?.Error("Error", "Failed to save configuration");
_toastService?.Warning("Warning", "This action cannot be undone");
_toastService?.Info("Info", "Process completed");
```

Toast levels: `Success`, `Info`, `Warning`, `Error`. Default auto-close: Success/Info/Warning = 4-6s, Error = manual.

### Card Styles

Cards are implemented as `Border` elements with style classes:

```xml
<Border Classes="card">          <!-- Standard card with border, hover effect -->
<Border Classes="section-card">  <!-- Subtle background, no border -->
<Border Classes="settings-card"> <!-- Card for settings sections -->
<Border Classes="list-card">     <!-- Card for list items, hover highlight -->
<Border Classes="empty-state">   <!-- Centered empty state container -->
```

### Button Styles

Button classes can be combined (variant + layout):

```xml
<!-- Variants -->
<Button Classes="primary">      <!-- Filled primary color -->
<Button Classes="secondary">    <!-- Outlined, primary color text -->
<Button Classes="destructive">  <!-- Red, for dangerous actions -->
<Button Classes="ghost">        <!-- Transparent, subtle hover -->

<!-- Layouts -->
<Button Classes="icon-button">       <!-- Square, icon only -->
<Button Classes="icon-text-button">  <!-- Icon + text horizontal -->

<!-- Example: Primary button with icon and text -->
<Button Classes="primary icon-text-button">
  <StackPanel>
    <mi:MaterialIcon Kind="Play" />
    <TextBlock Text="Start" />
  </StackPanel>
</Button>
```

### Text Styles

```xml
<TextBlock Classes="page-title">      <!-- 28px, semi-bold -->
<TextBlock Classes="page-subtitle">   <!-- 13px, muted -->
<TextBlock Classes="section-title">   <!-- 13px, semi-bold -->
<TextBlock Classes="card-title">      <!-- 16px, semi-bold -->
<TextBlock Classes="card-description"><!-- 13px, muted, wrapped -->
<TextBlock Classes="field-label">     <!-- Form field label -->
<TextBlock Classes="label">           <!-- Generic label -->
<TextBlock Classes="value">           <!-- Value paired with label -->
<TextBlock Classes="hint">            <!-- Small, muted hint text -->
```

### Page Layout

Standard page structure uses Border elements for layout sections:

```xml
<Grid RowDefinitions="Auto,Auto,*">
  <!-- Page Header -->
  <Border Grid.Row="0" Classes="page-header">
    <TextBlock Classes="page-title" Text="Page Title" />
  </Border>

  <!-- Page Toolbar (optional filters/actions) -->
  <Border Grid.Row="1" Classes="page-toolbar">
    <!-- Filters, action buttons -->
  </Border>

  <!-- Page Content -->
  <Border Grid.Row="2" Classes="page-content">
    <!-- Main content, typically cards -->
  </Border>
</Grid>
```

### Log Viewer

For displaying logs with level-based coloring:

```xml
<ListBox Classes="logViewer" ItemsSource="{Binding LogEntries}">
  <ListBox.ItemTemplate>
    <DataTemplate>
      <StackPanel Orientation="Horizontal">
        <TextBlock Text="{Binding Timestamp, StringFormat='[{0:HH:mm:ss.fff}]'}"
                   Classes="log-timestamp" />
        <TextBlock Text="{Binding Level}" Classes="log-level" />
        <TextBlock Text="{Binding Message}" />
      </StackPanel>
    </DataTemplate>
  </ListBox.ItemTemplate>
</ListBox>
```

ListBoxItem classes for log levels: `log-debug`, `log-info`, `log-warning`, `log-error`

### Status Indicators

```xml
<!-- Status dot (running/stopped/error) -->
<Border Classes="status-indicator running" />  <!-- Green -->
<Border Classes="status-indicator stopped" />  <!-- Gray -->
<Border Classes="status-indicator error" />    <!-- Red -->

<!-- Type badge -->
<Border Classes="type-badge">
  <TextBlock Text="TCP" />
</Border>
```

### Form Controls

```xml
<TextBox Classes="field-control" />
<ComboBox Classes="filter-box" />       <!-- For filters in toolbars -->
<ComboBox Classes="settings-dropdown" /> <!-- For settings pages -->
<CheckBox Classes="settings-checkbox" />

<!-- Form field container -->
<Border Classes="form-field">
  <TextBlock Classes="form-label">Label</TextBlock>
  <TextBox />
  <TextBlock Classes="form-hint">Hint text</TextBlock>
</Border>
```

### Design Tokens

Key tokens from `DesignTokens.axaml`:

- **Spacing**: `SpacingXS` (4), `SpacingS` (8), `SpacingM` (12), `SpacingL` (16), `SpacingXL` (24)
- **Font Sizes**: `FontSizeS` (11), `FontSizeM` (13), `FontSizeBase` (14), `FontSizeL` (16)
- **Corner Radius**: `CornerRadiusS` (4), `CornerRadiusM` (6), `CornerRadiusL` (8)
- **Content Padding**: `ContentPadding` (24)

## Project-Specific Terms

- `avalonia` - The UI framework
- `clonia` - Project name suffix
- `frapa` - Project name prefix
- `frpc` - Fast reverse proxy client (the underlying tool this UI wraps)
