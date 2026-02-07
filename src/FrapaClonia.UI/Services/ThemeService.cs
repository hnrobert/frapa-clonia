using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace FrapaClonia.UI.Services;

/// <summary>
/// Service for managing application theme
/// </summary>
public partial class ThemeService : ObservableObject
{
    private readonly ILogger<ThemeService> _logger;

    [ObservableProperty]
    private ThemeVariant _currentTheme = ThemeVariant.Default;

    [ObservableProperty]
    private bool _isLightTheme;

    [ObservableProperty]
    private bool _isDarkTheme;

    [ObservableProperty]
    private bool _isSystemTheme;

    public ThemeService(ILogger<ThemeService> logger)
    {
        _logger = logger;
        UpdateThemeFlags();
    }

    partial void OnCurrentThemeChanged(ThemeVariant value)
    {
        UpdateThemeFlags();
        ApplyTheme(value);
        _logger.LogInformation("Theme changed to: {Theme}", value);
    }

    private void UpdateThemeFlags()
    {
        IsLightTheme = CurrentTheme == ThemeVariant.Light;
        IsDarkTheme = CurrentTheme == ThemeVariant.Dark;
        IsSystemTheme = CurrentTheme == ThemeVariant.Default;
    }

    public void SetLightTheme()
    {
        CurrentTheme = ThemeVariant.Light;
    }

    public void SetDarkTheme()
    {
        CurrentTheme = ThemeVariant.Dark;
    }

    public void SetSystemTheme()
    {
        CurrentTheme = ThemeVariant.Default;
    }

    public void ToggleTheme()
    {
        if (CurrentTheme == ThemeVariant.Light)
            CurrentTheme = ThemeVariant.Dark;
        else if (CurrentTheme == ThemeVariant.Dark)
            CurrentTheme = ThemeVariant.Light;
        else
            CurrentTheme = Application.Current?.ActualThemeVariant == ThemeVariant.Light ? ThemeVariant.Dark : ThemeVariant.Light;
    }

    private static void ApplyTheme(ThemeVariant theme)
    {
        if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            foreach (var window in desktop.Windows)
            {
                window.RequestedThemeVariant = theme;
            }
        }
    }
}
