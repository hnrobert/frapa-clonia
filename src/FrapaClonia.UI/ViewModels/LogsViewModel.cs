using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrapaClonia.Core.Interfaces;
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

    // ReSharper disable once NotAccessedField.Local
    private readonly IConfigurationService? _configurationService;

    [ObservableProperty] private ObservableCollection<LogEntry> _logEntries = [];

    [ObservableProperty] private string _selectedLogLevel = "All";

    [ObservableProperty] private bool _isFollowEnabled = true;

    [ObservableProperty] private bool _isClearing;

    [ObservableProperty] private int _maxLogEntries = 1000;

    [ObservableProperty] private string _statusMessage = "Waiting for logs...";

    private readonly Queue<LogEntry> _logBuffer = new();
    private const int MaxBufferSize = 10000;

    public IRelayCommand ClearLogsCommand { get; }
    public IRelayCommand ExportLogsCommand { get; }
    public IRelayCommand ToggleFollowCommand { get; }
    public IRelayCommand RefreshCommand { get; }

    public List<string> LogLevels { get; } = ["All", "Debug", "Information", "Warning", "Error"];

    // Default constructor for design-time support
    public LogsViewModel() : this(
        Microsoft.Extensions.Logging.Abstractions.NullLogger<LogsViewModel>.Instance,
        null!,
        null!)
    {
    }

    public LogsViewModel(
        ILogger<LogsViewModel> logger,
        IFrpcProcessService frpcProcessService,
        IConfigurationService configurationService)
    {
        _logger = logger;
        _frpcProcessService = frpcProcessService;
        _configurationService = configurationService;

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
                _logger?.LogError(e, "Error clearing logs");
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
                _logger?.LogError(e, "Error clearing logs");
            }
        });

        // Subscribe to log events
        _frpcProcessService.LogLineReceived += OnLogLineReceived;
        _frpcProcessService.ProcessStateChanged += OnProcessStateChanged;

        // Update initial status
        UpdateStatus();
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