using JsonMonitor.Core.Models;
using Microsoft.Extensions.Logging;

namespace JsonMonitor.Core.Services;

/// <summary>
/// Implementation of file monitoring service using PeriodicTimer.
/// Monitors a JSON file for changes at specified intervals and raises events accordingly.
/// </summary>
public class FileMonitoringService : IFileMonitoringService, IDisposable
{
    private readonly IJsonFileService _jsonFileService;
    private readonly ILogger<FileMonitoringService> _logger;
    
    private PeriodicTimer? _timer;
    private DateTime _lastKnownModificationTime;
    private string? _currentFilePath;
    private bool _isMonitoring;
    private bool _disposed;

    public FileMonitoringService(IJsonFileService jsonFileService, ILogger<FileMonitoringService> logger)
    {
        _jsonFileService = jsonFileService ?? throw new ArgumentNullException(nameof(jsonFileService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public event EventHandler<FileChangedEventArgs>? FileChanged;

    public event EventHandler<DataLoadedEventArgs>? DataLoaded;

    public event EventHandler<MonitoringErrorEventArgs>? MonitoringError;

    public event EventHandler<MonitoringStatusChangedEventArgs>? MonitoringStatusChanged;

    public bool IsMonitoring => _isMonitoring;

    public string? CurrentFilePath => _currentFilePath;

    public async Task StartMonitoringAsync(string filePath, int intervalSeconds = 2, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(FileMonitoringService));
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
        }

        if (intervalSeconds <= 0)
        {
            throw new ArgumentException("Interval must be positive", nameof(intervalSeconds));
        }

        if (_isMonitoring)
        {
            StopMonitoring();
        }

        _currentFilePath = filePath;
        _lastKnownModificationTime = _jsonFileService.GetLastWriteTime(filePath);
        _isMonitoring = true;

        _logger.LogInformation("Starting file monitoring for: {FilePath} with interval: {IntervalSeconds}s", 
            filePath, intervalSeconds);

        OnMonitoringStatusChanged(new MonitoringStatusChangedEventArgs(true, filePath));

        await ForceRefreshAsync(cancellationToken: cancellationToken);

        _timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds));
        
        try
        {
            while (await _timer.WaitForNextTickAsync(cancellationToken))
            {
                if (!_isMonitoring || cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await CheckForChangesAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("File monitoring was cancelled for: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during file monitoring for: {FilePath}", filePath);
            OnMonitoringError(new MonitoringErrorEventArgs(ex, filePath));
        }
        finally
        {
            StopMonitoring();
        }
    }

    public async Task ForceRefreshAsync(string? filePath = null, CancellationToken cancellationToken = default)
    {
        var targetPath = filePath ?? _currentFilePath;
        
        if (string.IsNullOrEmpty(targetPath))
        {
            _logger.LogWarning("Cannot force refresh - no file path provided");
            return;
        }

        try
        {
            var data = await _jsonFileService.ReadJsonFileAsync(targetPath, cancellationToken);
            OnDataLoaded(new DataLoadedEventArgs(data, true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading data from file: {FilePath}", targetPath);
            OnMonitoringError(new MonitoringErrorEventArgs(ex, targetPath));
        }
    }

    public async Task<bool> CheckForChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed || string.IsNullOrEmpty(_currentFilePath))
        {
            return false;
        }

        try
        {
            var currentModificationTime = _jsonFileService.GetLastWriteTime(_currentFilePath);
            bool hasChanged = currentModificationTime > _lastKnownModificationTime;

            OnFileChanged(new FileChangedEventArgs(_currentFilePath, currentModificationTime, hasChanged));

            if (hasChanged)
            {
                _lastKnownModificationTime = currentModificationTime;
                await ForceRefreshAsync(cancellationToken: cancellationToken);
            }

            return hasChanged;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for file changes: {FilePath}", _currentFilePath);
            OnMonitoringError(new MonitoringErrorEventArgs(ex, _currentFilePath));
            return false;
        }
    }

    private void StopMonitoring()
    {
        if (_isMonitoring)
        {
            _isMonitoring = false;
            OnMonitoringStatusChanged(new MonitoringStatusChangedEventArgs(false, _currentFilePath));
        }
        
        if (_timer != null)
        {
            _timer.Dispose();
            _timer = null;
        }
    }

    #region Protected Virtual Event Methods

    /// <summary>
    /// Raises the FileChanged event.
    /// </summary>
    /// <param name="e">Event arguments containing file change information.</param>
    protected virtual void OnFileChanged(FileChangedEventArgs e)
    {
        FileChanged?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the DataLoaded event.
    /// </summary>
    /// <param name="e">Event arguments containing loaded data information.</param>
    protected virtual void OnDataLoaded(DataLoadedEventArgs e)
    {
        DataLoaded?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the MonitoringError event.
    /// </summary>
    /// <param name="e">Event arguments containing error information.</param>
    protected virtual void OnMonitoringError(MonitoringErrorEventArgs e)
    {
        MonitoringError?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the MonitoringStatusChanged event.
    /// </summary>
    /// <param name="e">Event arguments containing status change information.</param>
    protected virtual void OnMonitoringStatusChanged(MonitoringStatusChangedEventArgs e)
    {
        MonitoringStatusChanged?.Invoke(this, e);
    }

    #endregion

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _isMonitoring = false;
                _timer?.Dispose();
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}