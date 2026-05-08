using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrapaClonia.Shared.Interfaces;
using FrapaClonia.Shared.Models;
using FrapaClonia.UI.Services;
using Microsoft.Extensions.Logging;
using System.Text;

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
    private readonly ISystemServiceManager? _systemServiceManager;

    [ObservableProperty] private string _logText = "";

    [ObservableProperty] private string _searchQuery = "";

    [ObservableProperty] private int _searchMatchCount;

    [ObservableProperty] private string _selectedLogLevel = "All";

    [ObservableProperty] private bool _isFollowEnabled = true;

    [ObservableProperty] private bool _isLoading = true;

    [ObservableProperty] private bool _isClearConfirmOpen;

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
    private readonly Lock _textLock = new();
    private readonly StringBuilder _logTextBuilder = new();

    // File-based log tailing for service-managed frpc
    private readonly System.Timers.Timer? _fileLogTimer;
    private long _logFilePosition;
    private string? _serviceLogPath;
    private bool _isServiceLogActive;

    // Loading timeout
    private readonly System.Timers.Timer _loadingTimer;

    public IRelayCommand ClearLogsCommand { get; }
    public IRelayCommand ConfirmClearCommand { get; }
    public IRelayCommand ConfirmClearWithFileCommand { get; }
    public IRelayCommand CancelClearCommand { get; }
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
        null!,
        null!)
    {
    }

    public LogsViewModel(
        ILogger<LogsViewModel> logger,
        IFrpcProcessService frpcProcessService,
        IPresetService presetService,
        ToastService toastService,
        ILocalizationService localizationService,
        ISystemServiceManager? systemServiceManager)
    {
        _logger = logger;
        _frpcProcessService = frpcProcessService;
        _presetService = presetService;
        _toastService = toastService;
        _localizationService = localizationService;
        _systemServiceManager = systemServiceManager;

        ClearLogsCommand = new RelayCommand(() => { IsClearConfirmOpen = true; });
        ConfirmClearCommand = new RelayCommand(async void () =>
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
        CancelClearCommand = new RelayCommand(() => { IsClearConfirmOpen = false; });
        ConfirmClearWithFileCommand = new RelayCommand(async void () =>
        {
            try
            {
                await ClearLogsAsync(deleteFiles: true);
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
                _toastService?.Info("Refreshed", "Logs have been refreshed");
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

        // File log tailing timer
        _fileLogTimer = new System.Timers.Timer(500);
        _fileLogTimer.Elapsed += OnFileLogTimerElapsed;

        // Loading timeout — stop loading after 2s even if no logs arrive
        _loadingTimer = new System.Timers.Timer(2000) { AutoReset = false };
        _loadingTimer.Elapsed += (_, _) =>
            Avalonia.Threading.Dispatcher.UIThread.Post(() => IsLoading = false);
        _loadingTimer.Start();

        // Check if frpc is running as a service (no direct process)
        _ = UpdateLogSourceAsync();
    }

    private void OnCurrentPresetChanged(object? sender, PresetChangedEventArgs e)
    {
        LoadSettingsFromPreset();
        StopFileLogTailing();
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            lock (_logBuffer) { _logBuffer.Clear(); }
            lock (_textLock) { _logTextBuilder.Clear(); LogText = ""; }
            IsLoading = true;
            _loadingTimer.Start();
            _ = UpdateLogSourceAsync();
        });
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
            LogLevelIndex = 2;
            LogTo = "";
            LogMaxDaysText = "3";
        }
    }

    private void OpenSettings()
    {
        _originalLogLevelIndex = LogLevelIndex;
        _originalLogMaxDaysText = LogMaxDaysText;
        _originalLogTo = LogTo;
        IsSettingsOpen = true;
    }

    private void CancelSettings()
    {
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

    public void RefreshOnNavigate()
    {
        _ = UpdateLogSourceAsync();
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnSearchQueryChanged(string value)
    {
        RebuildLogText();
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnSelectedLogLevelChanged(string value)
    {
        RebuildLogText();
    }

    private void OnLogLineReceived(object? sender, LogLineEventArgs e)
    {
        var entry = new LogEntry
        {
            Timestamp = e.Timestamp,
            Level = MapLogLevel(e.LogLevel),
            Message = e.LogLine
        };

        AddLogEntry(entry);
    }

    private void OnProcessStateChanged(object? sender, ProcessStateChangedEventArgs e)
    {
        UpdateStatus();
        _ = UpdateLogSourceAsync();
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

    private void AddLogEntry(LogEntry entry)
    {
        lock (_logBuffer)
        {
            _logBuffer.Enqueue(entry);
            while (_logBuffer.Count > MaxBufferSize)
                _logBuffer.Dequeue();
        }

        if (!ShouldShowLog(entry)) return;

        var line = FormatEntry(entry);
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (IsLoading)
            {
                IsLoading = false;
                _loadingTimer.Stop();
            }

            lock (_textLock)
            {
                _logTextBuilder.AppendLine(line);
                LogText = _logTextBuilder.ToString();
            }

            UpdateStatus();
        });
    }

    private void RebuildLogText()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var sb = new StringBuilder();
            var searchQuery = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery;

            lock (_logBuffer)
            {
                foreach (var line in from entry in _logBuffer
                         where ShouldShowLog(entry)
                         select FormatEntry(entry)
                         into line
                         where searchQuery == null || line.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                         select line)
                {
                    sb.AppendLine(line);
                }
            }

            lock (_textLock)
            {
                _logTextBuilder.Clear();
                _logTextBuilder.Append(sb);
                LogText = sb.ToString();
            }

            SearchMatchCount = searchQuery != null
                ? LogText.Split(searchQuery).Length - 1
                : 0;
        });
    }

    private Task ClearLogsAsync(bool deleteFiles = false)
    {
        try
        {
            IsClearing = true;
            IsClearConfirmOpen = false;

            lock (_logBuffer)
            {
                _logBuffer.Clear();
            }

            lock (_textLock)
            {
                _logTextBuilder.Clear();
                LogText = "";
            }

            if (deleteFiles)
            {
                // Delete service log files
                if (!string.IsNullOrEmpty(_serviceLogPath))
                {
                    var errPath = Path.ChangeExtension(_serviceLogPath, ".err");
                    if (File.Exists(_serviceLogPath)) File.Delete(_serviceLogPath);
                    if (File.Exists(errPath)) File.Delete(errPath);
                    _logFilePosition = 0;
                }

                _toastService?.Success("Cleared", "Logs and log files have been cleared");
            }
            else
            {
                _toastService?.Success("Cleared", "Panel logs have been cleared");
            }

            _logger?.LogInformation("Logs cleared (deleteFiles={DeleteFiles})", deleteFiles);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error clearing logs");
            _toastService?.Error("Clear Failed", "Could not clear logs");
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
            if (Avalonia.Application.Current!.ApplicationLifetime is not
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                return;

            var storageProvider = desktop.MainWindow?.StorageProvider;
            if (storageProvider == null) return;

            var file = await storageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Export Logs",
                SuggestedFileName = $"frapa-clonia-logs-{DateTime.Now:yyyyMMdd-HHmmss}.txt",
                DefaultExtension = "txt",
                FileTypeChoices =
                [
                    new Avalonia.Platform.Storage.FilePickerFileType("Text Files")
                    {
                        Patterns = ["*.txt"]
                    },
                    new Avalonia.Platform.Storage.FilePickerFileType("All Files")
                    {
                        Patterns = ["*"]
                    }
                ]
            });

            if (file == null) return;

            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(LogText);

            _toastService?.Success("Exported", $"Logs exported to {file.Name}");
            _logger?.LogInformation("Logs exported to {Path}", file.Path.LocalPath);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error exporting logs");
            _toastService?.Error("Export Failed", "Could not export logs");
        }
    }

    private async Task RefreshAsync()
    {
        RebuildLogText();
        await Task.CompletedTask;
    }

    private bool ShouldShowLog(LogEntry entry)
    {
        if (SelectedLogLevel == "All") return true;
        return entry.Level == SelectedLogLevel;
    }

    private static string FormatEntry(LogEntry entry) =>
        $"[{entry.Timestamp:HH:mm:ss.fff}] [{entry.Level.ToLowerInvariant()}] {entry.Message}";

    // --- File-based log tailing for service-managed frpc ---

    private async Task UpdateLogSourceAsync()
    {
        try
        {
            if (_frpcProcessService?.IsRunning == true)
            {
                StopFileLogTailing();
                return;
            }

            if (_systemServiceManager == null || _presetService?.CurrentPreset == null)
            {
                StopFileLogTailing();
                return;
            }

            var settings = _presetService.CurrentPreset.Deployment;
            if (settings.DeploymentMode == "docker")
            {
                StopFileLogTailing();
                return;
            }

            var serviceName = _systemServiceManager.GetServiceNameForPreset(_presetService.CurrentPreset.Id);
            var isInstalled = await _systemServiceManager.IsServiceInstalledAsync(serviceName);

            if (isInstalled)
            {
                StartFileLogTailing(serviceName);
            }
            else
            {
                StopFileLogTailing();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to check service status for log source");
        }
    }

    private void StartFileLogTailing(string serviceName)
    {
        if (_isServiceLogActive) return;

        _serviceLogPath = $"/tmp/{serviceName}.log";
        _logFilePosition = 0;
        _isServiceLogActive = true;
        _fileLogTimer?.Start();
        _logger?.LogInformation("Started tailing service log file: {Path}", _serviceLogPath);
    }

    private void StopFileLogTailing()
    {
        if (!_isServiceLogActive) return;

        _fileLogTimer?.Stop();
        _isServiceLogActive = false;
        _serviceLogPath = null;
        _logFilePosition = 0;
    }

    private void OnFileLogTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (string.IsNullOrEmpty(_serviceLogPath)) return;

        try
        {
            if (!File.Exists(_serviceLogPath))
                return;

            using var stream = new FileStream(_serviceLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (stream.Length < _logFilePosition)
                _logFilePosition = 0;

            stream.Seek(_logFilePosition, SeekOrigin.Begin);
            using var reader = new StreamReader(stream);

            while (reader.ReadLine() is { } line)
            {
                var (level, message) = ParseFrpcLogLine(line);
                var entry = new LogEntry
                {
                    Timestamp = DateTimeOffset.Now,
                    Level = level,
                    Message = message
                };

                AddLogEntry(entry);
            }

            _logFilePosition = stream.Position;
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Error reading service log file");
        }
    }

    private static (string Level, string Message) ParseFrpcLogLine(string line)
    {
        var level = "Information";
        var message = line;

        if (line.Length > 21)
        {
            var bracketStart = line.IndexOf('[', 20);
            if (bracketStart >= 0)
            {
                var bracketEnd = line.IndexOf(']', bracketStart);
                if (bracketEnd > bracketStart)
                {
                    var levelChar = line.Substring(bracketStart + 1, bracketEnd - bracketStart - 1);
                    level = levelChar.ToUpperInvariant() switch
                    {
                        "T" => "Trace",
                        "D" => "Debug",
                        "I" => "Information",
                        "W" => "Warning",
                        "E" => "Error",
                        _ => "Information"
                    };
                    message = line.Substring(bracketEnd + 1).TrimStart();
                }
            }
        }

        return (level, message);
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
/// Log entry for internal buffer
/// </summary>
public class LogEntry
{
    public DateTimeOffset Timestamp { get; init; }
    public string Level { get; init; } = "";
    public string Message { get; init; } = "";
}
