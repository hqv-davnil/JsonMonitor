using JsonMonitor.Core.Models;

namespace JsonMonitor.Core.Services;

/// <summary>
/// Interface for monitoring JSON file changes.
/// Provides events for file change notifications and supports cancellation.
/// </summary>
public interface IFileMonitoringService
{
    /// <summary>
    /// Event raised when the monitored file has changed.
    /// </summary>
    event EventHandler<FileChangedEventArgs>? FileChanged;

    /// <summary>
    /// Event raised when data is loaded from the file (either changed or forced refresh).
    /// </summary>
    event EventHandler<DataLoadedEventArgs>? DataLoaded;

    /// <summary>
    /// Event raised when an error occurs during monitoring or file reading.
    /// </summary>
    event EventHandler<MonitoringErrorEventArgs>? MonitoringError;

    /// <summary>
    /// Event raised when monitoring status changes (starts or stops).
    /// </summary>
    event EventHandler<MonitoringStatusChangedEventArgs>? MonitoringStatusChanged;

    /// <summary>
    /// Starts monitoring the specified file for changes using PeriodicTimer.
    /// </summary>
    /// <param name="filePath">Path to the JSON file to monitor.</param>
    /// <param name="intervalSeconds">Monitoring interval in seconds (default is 2).</param>
    /// <param name="cancellationToken">Token to cancel the monitoring operation.</param>
    /// <returns>A task representing the monitoring operation.</returns>
    Task StartMonitoringAsync(string filePath, int intervalSeconds = 2, CancellationToken cancellationToken = default);

    /// <summary>
    /// Forces a refresh of the file data, regardless of whether it has changed.
    /// </summary>
    /// <param name="filePath">Optional path to the JSON file to refresh. If null, uses current monitored file.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the refresh operation.</returns>
    Task ForceRefreshAsync(string? filePath = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the service is currently monitoring a file.
    /// </summary>
    bool IsMonitoring { get; }

    /// <summary>
    /// Gets the path of the currently monitored file.
    /// </summary>
    string? CurrentFilePath { get; }
}

/// <summary>
/// Event args for file change notifications.
/// </summary>
public class FileChangedEventArgs : EventArgs
{
    public string FilePath { get; }
    public DateTime LastModified { get; }
    public bool HasChanged { get; }

    public FileChangedEventArgs(string filePath, DateTime lastModified, bool hasChanged)
    {
        FilePath = filePath;
        LastModified = lastModified;
        HasChanged = hasChanged;
    }
}

/// <summary>
/// Event args for data loaded events.
/// </summary>
public class DataLoadedEventArgs : EventArgs
{
    public RootData? Data { get; }
    public bool WasForced { get; }

    public DataLoadedEventArgs(RootData? data, bool wasForced)
    {
        Data = data;
        WasForced = wasForced;
    }
}

/// <summary>
/// Event args for monitoring errors.
/// </summary>
public class MonitoringErrorEventArgs : EventArgs
{
    public Exception Exception { get; }
    public string? FilePath { get; }

    public MonitoringErrorEventArgs(Exception exception, string? filePath = null)
    {
        Exception = exception;
        FilePath = filePath;
    }
}

/// <summary>
/// Event args for monitoring status changes.
/// </summary>
public class MonitoringStatusChangedEventArgs : EventArgs
{
    public bool IsMonitoring { get; }
    public string? FilePath { get; }

    public MonitoringStatusChangedEventArgs(bool isMonitoring, string? filePath = null)
    {
        IsMonitoring = isMonitoring;
        FilePath = filePath;
    }
}