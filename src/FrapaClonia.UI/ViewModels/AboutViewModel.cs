using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrapaClonia.Shared.Interfaces;
using FrapaClonia.Shared.Utils;
using System.Diagnostics;

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
    [ObservableProperty] private string _projectUrl = "https://github.com/hnrobert/frapa-clonia";

    public string ProjectUrlShort
    {
        get
        {
            const string prefix = "github.com/";
            var index = ProjectUrl.IndexOf(prefix, StringComparison.Ordinal);
            return index >= 0 ? ProjectUrl[(index + prefix.Length)..] : ProjectUrl;
        }
    }

    public event EventHandler? CloseRequested;

    public AboutViewModel(ILocalizationService? localizationService)
    {
        _localizationService = localizationService;

        // Set title
        Title = L("About") + " FrapaClonia";

        // Get version and copyright from utility
        Version = AppVersion.Version;
        Copyright = AppVersion.Copyright;

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
