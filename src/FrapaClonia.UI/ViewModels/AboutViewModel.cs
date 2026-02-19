using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrapaClonia.Core.Interfaces;
using System.Diagnostics;
using System.Reflection;

namespace FrapaClonia.UI.ViewModels;

/// <summary>
/// ViewModel for the About dialog
/// </summary>
public partial class AboutViewModel : ObservableObject
{
    private readonly ILocalizationService? _localizationService;

    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _version = string.Empty;
    [ObservableProperty] private string _copyright = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private string _projectUrl = "https://github.com/anthropics/claude-code";

    public event EventHandler? CloseRequested;

    public AboutViewModel() : this(null) { }

    public AboutViewModel(ILocalizationService? localizationService)
    {
        _localizationService = localizationService;

        // Set title
        Title = L("About") + " FrapaClonia";

        // Get version from assembly
        var assembly = Assembly.GetExecutingAssembly();
        var versionAttr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        Version = versionAttr?.InformationalVersion ?? "1.0.0";

        // Set copyright
        var copyrightAttr = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>();
        Copyright = copyrightAttr?.Copyright ?? "© 2024 FrapaClonia";

        // Set description
        Description = L("AboutDescription");
    }

    private string L(string key) => _localizationService?.GetString(key) ?? key;

    [RelayCommand]
    private void Close()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void OpenProjectUrl()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = ProjectUrl,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore errors when opening URL
        }
    }
}
