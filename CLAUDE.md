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

## Project-Specific Terms

- `avalonia` - The UI framework
- `clonia` - Project name suffix
- `frapa` - Project name prefix
- `frpc` - Fast reverse proxy client (the underlying tool this UI wraps)
