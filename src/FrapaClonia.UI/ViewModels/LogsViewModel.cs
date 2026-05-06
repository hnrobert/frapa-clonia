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
    private readonly object _textLock = new();
    private readonly StringBuilder _logTextBuilder = new();

    // File-based log tailing for service-managed frpc
    private System.Timers.Timer? _fileLogTimer;
    private long _logFilePosition;
    private string? _serviceLogPath;
    private bool _isServiceLogActive;

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

        ClearLogsCommand = new RelayCommand(async void () =>
        {
            try { await ClearLogsAsync(); }
            catch (Exception e) { _logger?.LogError(e, "Error clearing logs"); }
        });
        ExportLogsCommand = new RelayCommand(async void () =>
        {
            try { await ExportLogsAsync(); }
            catch (Exception e) { _logger?.LogError(e, "Error exporting logs"); }
        });
        ToggleFollowCommand = new RelayCommand(ToggleFollow);
        RefreshCommand = new RelayCommand(async void () =>
        {
            try { await RefreshAsync(); }
            catch (Exception e) { _logger?.LogError(e, "Error refreshing logs"); }
        });
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        SaveSettingsCommand = new RelayCommand(async void () =>
        {
            try { await SaveSettingsAsync(); }
            catch (Exception e) { _logger?.LogError(e, "Error saving log settings"); }
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

        // Check if frpc is running as a service (no direct process)
        _ = UpdateLogSourceAsync();
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

    partial void OnSearchQueryChanged(string value)
    {
        RebuildLogText();
    }

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
                foreach (var entry in _logBuffer)
                {
                    if (!ShouldShowLog(entry)) continue;
                    var line = FormatEntry(entry);
                    if (searchQuery != null && !line.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                        continue;
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
                ? LogText.Split(searchQuery, StringSplitOptions.None).Length - 1
                : 0;
        });
    }

    private Task ClearLogsAsync()
    {
        try
        {
            IsClearing = true;
            lock (_logBuffer) { _logBuffer.Clear(); }
            lock (_textLock)
            {
                _logTextBuilder.Clear();
                LogText = "";
            }
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

            await File.WriteAllTextAsync(logsPath, LogText);

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

        try
        {
            var serviceName = _systemServiceManager.GetServiceNameForPreset(_presetService.CurrentPreset.Name);
            var isInstalled = await _systemServiceManager.IsServiceInstalledAsync(serviceName);

            if (!isInstalled)
            {
                var defaultName = _systemServiceManager.GetDefaultServiceName();
                if (await _systemServiceManager.IsServiceInstalledAsync(defaultName))
                {
                    serviceName = defaultName;
                    isInstalled = true;
                }
            }

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

            string? line;
            while ((line = reader.ReadLine()) != null)
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
