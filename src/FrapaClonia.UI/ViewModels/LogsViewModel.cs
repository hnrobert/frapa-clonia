using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrapaClonia.Core.Interfaces;
using FrapaClonia.Domain.Models;
using FrapaClonia.UI.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace FrapaClonia.UI.ViewModels;

/// <summary>
/// View model for logs display
/// </summary>
public partial class LogsViewModel : ObservableObject
{
    private readonly ILogger<LogsViewModel>? _logger;
    private readonly IFrpcProcessService? _frpcProcessService;
    private readonly IPresetService? _presetService;
    private readonly ToastService? _toastService;
    private readonly ILocalizationService? _localizationService;

    [ObservableProperty] private ObservableCollection<LogEntry> _logEntries = [];

    [ObservableProperty] private string _selectedLogLevel = "All";

    [ObservableProperty] private bool _isFollowEnabled = true;

    [ObservableProperty] private bool _isClearing;

    [ObservableProperty] private int _maxLogEntries = 1000;

    [ObservableProperty] private string _statusMessage = "Waiting for logs...";

    [ObservableProperty] private bool _isSettingsOpen;

    // Logging settings (for dialog)
    [ObservableProperty] private int _logLevelIndex;
    [ObservableProperty] private string _logMaxDaysText = "3";
    [ObservableProperty] private string _logTo = "";

    // Temporary settings for cancel functionality
    private int _originalLogLevelIndex;
    private string _originalLogMaxDaysText = "3";
    private string _originalLogTo = "";

    private readonly Queue<LogEntry> _logBuffer = new();
    private const int MaxBufferSize = 10000;

    public IRelayCommand ClearLogsCommand { get; }
    public IRelayCommand ExportLogsCommand { get; }
    public IRelayCommand ToggleFollowCommand { get; }
    public IRelayCommand RefreshCommand { get; }
    public IRelayCommand OpenSettingsCommand { get; }
    public IRelayCommand SaveSettingsCommand { get; }
    public IRelayCommand CancelSettingsCommand { get; }

    public List<string> LogLevels { get; } = ["All", "Debug", "Information", "Warning", "Error"];

    private string L(string key, params object[] args) =>
        _localizationService?.GetString(key, args) ?? key;

    // Default constructor for design-time support
    public LogsViewModel() : this(
        Microsoft.Extensions.Logging.Abstractions.NullLogger<LogsViewModel>.Instance,
        null!,
        null!,
        null!,
        null!)
    {
    }

    public LogsViewModel(
        ILogger<LogsViewModel> logger,
        IFrpcProcessService frpcProcessService,
        IPresetService presetService,
        ToastService toastService,
        ILocalizationService localizationService)
    {
        _logger = logger;
        _frpcProcessService = frpcProcessService;
        _presetService = presetService;
        _toastService = toastService;
        _localizationService = localizationService;

        ClearLogsCommand = new RelayCommand(async void () =>
        {
            try
            {
                await ClearLogsAsync();
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error clearing logs");
            }
        });
        ExportLogsCommand = new RelayCommand(async void () =>
        {
            try
            {
                await ExportLogsAsync();
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error exporting logs");
            }
        });
        ToggleFollowCommand = new RelayCommand(ToggleFollow);
        RefreshCommand = new RelayCommand(async void () =>
        {
            try
            {
                await RefreshAsync();
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error refreshing logs");
            }
        });
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        SaveSettingsCommand = new RelayCommand(async void () =>
        {
            try
            {
                await SaveSettingsAsync();
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error saving log settings");
            }
        });
        CancelSettingsCommand = new RelayCommand(CancelSettings);

        // Subscribe to log events
        _frpcProcessService.LogLineReceived += OnLogLineReceived;
        _frpcProcessService.ProcessStateChanged += OnProcessStateChanged;

        // Subscribe to preset changes
        if (_presetService != null)
        {
            _presetService.CurrentPresetChanged += OnCurrentPresetChanged;
        }

        // Update initial status
        UpdateStatus();
        LoadSettingsFromPreset();
    }

    private void OnCurrentPresetChanged(object? sender, PresetChangedEventArgs e)
    {
        LoadSettingsFromPreset();
    }

    private void LoadSettingsFromPreset()
    {
        if (_presetService?.CurrentPreset?.Configuration.CommonConfig?.Log is { } log)
        {
            LogLevelIndex = log.Level.ToLowerInvariant() switch
            {
                "trace" => 0,
                "debug" => 1,
                "info" => 2,
                "warn" => 3,
                "error" => 4,
                _ => 2
            };
            LogTo = log.To ?? "";
            LogMaxDaysText = log.MaxDays.ToString();
        }
        else
        {
            LogLevelIndex = 2; // Info
            LogTo = "";
            LogMaxDaysText = "3";
        }
    }

    private void OpenSettings()
    {
        // Store original values for cancel
        _originalLogLevelIndex = LogLevelIndex;
        _originalLogMaxDaysText = LogMaxDaysText;
        _originalLogTo = LogTo;
        IsSettingsOpen = true;
    }

    private void CancelSettings()
    {
        // Restore original values
        LogLevelIndex = _originalLogLevelIndex;
        LogMaxDaysText = _originalLogMaxDaysText;
        LogTo = _originalLogTo;
        IsSettingsOpen = false;
    }

    private async Task SaveSettingsAsync()
    {
        if (_presetService?.CurrentPreset == null) return;

        var config = _presetService.CurrentPreset.Configuration;
        config.CommonConfig ??= new ClientCommonConfig();
        config.CommonConfig.Log = new LogConfig
        {
            Level = LogLevelIndex switch
            {
                0 => "trace",
                1 => "debug",
                2 => "info",
                3 => "warn",
                4 => "error",
                _ => "info"
            },
            To = LogTo,
            MaxDays = int.TryParse(LogMaxDaysText, out var days) ? days : 3
        };

        await _presetService.SaveCurrentPresetAsync();
        _toastService?.Success(L("Toast_Saved"), L("Toast_LogSettingsSaved"));
        IsSettingsOpen = false;
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnSelectedLogLevelChanged(string value)
    {
        // Filter logs based on selected level
        Task.Run(FilterLogsAsync);
    }

    private void OnLogLineReceived(object? sender, LogLineEventArgs e)
    {
        var entry = new LogEntry
        {
            Timestamp = e.Timestamp,
            Level = MapLogLevel(e.LogLevel),
            Message = e.LogLine
        };

        // Add to buffer
        lock (_logBuffer)
        {
            _logBuffer.Enqueue(entry);
            while (_logBuffer.Count > MaxBufferSize)
            {
                _logBuffer.Dequeue();
            }
        }

        // Add to visible collection if matches filter
        if (ShouldShowLog(entry))
        {
            // Dispatch to UI thread
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                LogEntries.Add(entry);
                // Trim to max entries
                while (LogEntries.Count > MaxLogEntries)
                {
                    LogEntries.RemoveAt(0);
                }
            });
        }
    }

    private void OnProcessStateChanged(object? sender, ProcessStateChangedEventArgs e)
    {
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        StatusMessage = _frpcProcessService?.IsRunning == true
            ? $"Connected to frpc (PID: {_frpcProcessService.ProcessId}) - Receiving logs..."
            : "frpc is not running - Start frpc to see logs";
    }

    private void ToggleFollow()
    {
        IsFollowEnabled = !IsFollowEnabled;
        _logger?.LogInformation("Log follow {State}", IsFollowEnabled ? "enabled" : "disabled");
    }

    private Task ClearLogsAsync()
    {
        try
        {
            IsClearing = true;
            LogEntries.Clear();
            _logger?.LogInformation("Logs cleared");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error clearing logs");
        }
        finally
        {
            IsClearing = false;
        }

        return Task.CompletedTask;
    }

    private async Task ExportLogsAsync()
    {
        try
        {
            var logsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                $"frapa-clonia-logs-{DateTime.Now:yyyyMMdd-HHmmss}.txt");

            await File.WriteAllLinesAsync(logsPath, LogEntries.Select(e =>
                $"[{e.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{e.Level}] {e.Message}"));

            StatusMessage = $"Logs exported to: {logsPath}";
            _logger?.LogInformation("Logs exported to {Path}", logsPath);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error exporting logs");
            StatusMessage = "Error exporting logs";
        }
    }

    private async Task RefreshAsync()
    {
        await Task.Run(() =>
        {
            // Reload logs from buffer with current filter
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                LogEntries.Clear();
                lock (_logBuffer)
                {
                    foreach (var entry in _logBuffer)
                    {
                        if (ShouldShowLog(entry))
                        {
                            LogEntries.Add(entry);
                        }
                    }
                }
            });
        });
    }

    private async Task FilterLogsAsync()
    {
        await Task.Run(() =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                var filtered = new ObservableCollection<LogEntry>();
                lock (_logBuffer)
                {
                    foreach (var entry in _logBuffer.Where(ShouldShowLog))
                    {
                        filtered.Add(entry);
                    }
                }

                LogEntries = filtered;
            });
        });
    }

    private bool ShouldShowLog(LogEntry entry)
    {
        if (SelectedLogLevel == "All") return true;
        return entry.Level == SelectedLogLevel;
    }

    private static string MapLogLevel(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "Trace",
            LogLevel.Debug => "Debug",
            LogLevel.Information => "Information",
            LogLevel.Warning => "Warning",
            LogLevel.Error => "Error",
            LogLevel.Critical => "Critical",
            _ => "None"
        };
    }
}

/// <summary>
/// Log entry for display
/// </summary>
public class LogEntry
{
    public DateTimeOffset Timestamp { get; init; }
    public string Level { get; init; } = "";
    public string Message { get; init; } = "";
}
