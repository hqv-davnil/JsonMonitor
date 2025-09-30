using JsonMonitor.Core.Models;
using JsonMonitor.Core.Services;
using JsonMonitor.WpfApp.Commands;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;

namespace JsonMonitor.WpfApp.ViewModels;

/// <summary>
/// Main ViewModel for garden tools connection monitoring application
/// </summary>
public class MainViewModel : ViewModelBase, IDisposable
{
    private readonly IFileMonitoringService _fileMonitoringService;
    private readonly ILogger<MainViewModel> _logger;
    private CancellationTokenSource _cancellationTokenSource;

    private string _title = string.Empty;
    private DateTime _lastModified;
    private string _statusMessage = "Ready";
    private bool _isMonitoring;
    private bool _isRefreshing;
    private string _filePath = string.Empty;
    private ObservableCollection<DataItem> _items = new();
    private bool _disposed;

    public MainViewModel(IFileMonitoringService fileMonitoringService, ILogger<MainViewModel> logger)
    {
        _fileMonitoringService = fileMonitoringService ?? throw new ArgumentNullException(nameof(fileMonitoringService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cancellationTokenSource = new CancellationTokenSource();

        RefreshCommand = new AsyncRelayCommand(ExecuteRefreshAsync, CanExecuteRefresh);
        StartMonitoringCommand = new AsyncRelayCommand(ExecuteStartMonitoringAsync, CanExecuteStartMonitoring);
        StopMonitoringCommand = new AsyncRelayCommand(ExecuteStopMonitoringAsync, CanExecuteStopMonitoring);

        _fileMonitoringService.DataLoaded += OnDataLoaded;
        _fileMonitoringService.FileChanged += OnFileChanged;
        _fileMonitoringService.MonitoringError += OnMonitoringError;
        _fileMonitoringService.MonitoringStatusChanged += OnMonitoringStatusChanged;

        _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data.json");
    }

    #region Properties

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public DateTime LastModified
    {
        get => _lastModified;
        set => SetProperty(ref _lastModified, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsMonitoring
    {
        get => _isMonitoring;
        set 
        { 
            if (SetProperty(ref _isMonitoring, value))
            {
                if (RefreshCommand is AsyncRelayCommand refreshCmd)
                    refreshCmd.RaiseCanExecuteChanged();
                if (StartMonitoringCommand is AsyncRelayCommand startCmd)
                    startCmd.RaiseCanExecuteChanged();
                if (StopMonitoringCommand is AsyncRelayCommand stopCmd)
                    stopCmd.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        set 
        { 
            if (SetProperty(ref _isRefreshing, value))
            {
                if (RefreshCommand is AsyncRelayCommand refreshCmd)
                    refreshCmd.RaiseCanExecuteChanged();
            }
        }
    }

    public string FilePath
    {
        get => _filePath;
        set 
        { 
            if (SetProperty(ref _filePath, value))
            {
                if (RefreshCommand is AsyncRelayCommand refreshCmd)
                    refreshCmd.RaiseCanExecuteChanged();
                if (StartMonitoringCommand is AsyncRelayCommand startCmd)
                    startCmd.RaiseCanExecuteChanged();
            }
        }
    }

    public ObservableCollection<DataItem> Items
    {
        get => _items;
        private set => SetProperty(ref _items, value);
    }

    #endregion

    #region Commands

    public ICommand RefreshCommand { get; }
    public ICommand StartMonitoringCommand { get; }
    public ICommand StopMonitoringCommand { get; }

    #endregion

    #region Public Methods

    public async Task InitializeAsync()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MainViewModel));
        }

        try
        {
            StatusMessage = "Initializing...";
            _logger.LogInformation("Initializing MainViewModel with file: {FilePath}", FilePath);

            if (File.Exists(FilePath))
            {
                await _fileMonitoringService.ForceRefreshAsync(FilePath, _cancellationTokenSource.Token);
                StatusMessage = "Ready - Click 'Start Monitoring' to begin file monitoring";
            }
            else
            {
                StatusMessage = $"File not found: {Path.GetFileName(FilePath)}";
                _logger.LogWarning("JSON file not found at: {FilePath}", FilePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing MainViewModel");
            StatusMessage = $"Initialization error: {ex.Message}";
        }
    }

    #endregion

    #region Command Implementations

    private async Task ExecuteRefreshAsync(object? parameter)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            IsRefreshing = true;
            StatusMessage = "Refreshing...";
            
            // Force refresh the file data
            await _fileMonitoringService.ForceRefreshAsync(FilePath, _cancellationTokenSource.Token);
            
            StatusMessage = "Refresh completed";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Refresh cancelled";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Refresh error: {ex.Message}";
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private bool CanExecuteRefresh(object? parameter)
    {
        var canExecute = !IsRefreshing && !_disposed && File.Exists(FilePath);
        return canExecute;
    }

    private async Task ExecuteStartMonitoringAsync(object? parameter)
    {
        if (_disposed)
        {
            return;
        }

        await StartMonitoringInternalAsync();
    }

    private bool CanExecuteStartMonitoring(object? parameter)
    {
        return !IsMonitoring && !_disposed && File.Exists(FilePath);
    }

    private async Task ExecuteStopMonitoringAsync(object? parameter)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            StatusMessage = "Stopping monitoring...";
            _logger.LogInformation("Stopping file monitoring");
            
            _cancellationTokenSource.Cancel();
            
            await Task.Delay(500);
            
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            
            IsMonitoring = false;
            StatusMessage = "Monitoring stopped";
            
            _logger.LogInformation("File monitoring stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping file monitoring");
            StatusMessage = $"Stop error: {ex.Message}";
            IsMonitoring = false;
        }
    }

    private bool CanExecuteStopMonitoring(object? parameter)
    {
        return IsMonitoring && !_disposed;
    }

    #endregion

    #region Private Methods

    private async Task StartMonitoringInternalAsync()
    {
        try
        {
            StatusMessage = "Starting monitoring...";
            _logger.LogInformation("Starting file monitoring for: {FilePath}", FilePath);
            
            IsMonitoring = true;
            
            await _fileMonitoringService.StartMonitoringAsync(FilePath, 2, _cancellationTokenSource.Token);
            
            _logger.LogInformation("File monitoring completed");
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("File monitoring was cancelled");
            StatusMessage = "Monitoring cancelled";
            IsMonitoring = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting file monitoring");
            StatusMessage = $"Monitoring error: {ex.Message}";
            IsMonitoring = false;
        }
    }

    private void UpdateDataFromRootData(RootData? rootData)
    {
        if (rootData == null)
        {
            Title = "No Data";
            LastModified = DateTime.MinValue;
            Items.Clear();
            return;
        }

        Title = rootData.Title;
        LastModified = rootData.LastModified;

        Items.Clear();
        if (rootData.Items != null)
        {
            foreach (var item in rootData.Items)
            {
                Items.Add(item);
            }
        }
    }

    #endregion

    #region Event Handlers

    private void OnDataLoaded(object? sender, DataLoadedEventArgs e)
    {
        if (System.Windows.Application.Current.Dispatcher.CheckAccess())
        {
            UpdateDataFromRootData(e.Data);
            StatusMessage = e.WasForced ? "Data refreshed manually" : "Data updated from file";
            
            IsMonitoring = _fileMonitoringService.IsMonitoring;
        }
        else
        {
            _logger.LogDebug("OnDataLoaded called on background thread - dispatching to UI thread");
            System.Windows.Application.Current.Dispatcher.Invoke(() => OnDataLoaded(sender, e));
        }
    }

    private void OnFileChanged(object? sender, FileChangedEventArgs e)
    {
        if (System.Windows.Application.Current.Dispatcher.CheckAccess())
        {
            if (e.HasChanged)
            {
                StatusMessage = "File changed, loading data...";
                _logger.LogDebug("File change detected: {FilePath}", e.FilePath);
            }
        }
        else
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => OnFileChanged(sender, e));
        }
    }

    private void OnMonitoringError(object? sender, MonitoringErrorEventArgs e)
    {
        if (System.Windows.Application.Current.Dispatcher.CheckAccess())
        {
            StatusMessage = $"Error: {e.Exception.Message}";
            _logger.LogError(e.Exception, "Monitoring error occurred");
        }
        else
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => OnMonitoringError(sender, e));
        }
    }

    private void OnMonitoringStatusChanged(object? sender, MonitoringStatusChangedEventArgs e)
    {
        if (System.Windows.Application.Current.Dispatcher.CheckAccess())
        {
            IsMonitoring = e.IsMonitoring;
            StatusMessage = e.IsMonitoring ? "Monitoring started" : "Monitoring stopped";
            _logger.LogDebug("Monitoring status changed: {IsMonitoring}", e.IsMonitoring);
        }
        else
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => OnMonitoringStatusChanged(sender, e));
        }
    }

    #endregion

    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _fileMonitoringService.DataLoaded -= OnDataLoaded;
                _fileMonitoringService.FileChanged -= OnFileChanged;
                _fileMonitoringService.MonitoringError -= OnMonitoringError;
                _fileMonitoringService.MonitoringStatusChanged -= OnMonitoringStatusChanged;

                _cancellationTokenSource.Cancel();
                
                _cancellationTokenSource.Dispose();
                
                if (_fileMonitoringService is IDisposable disposableService)
                {
                    disposableService.Dispose();
                }
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}