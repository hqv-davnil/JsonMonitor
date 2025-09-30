using JsonMonitor.Core.Services;
using JsonMonitor.WpfApp.ViewModels;
using JsonMonitor.WpfApp.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace JsonMonitor.WpfApp;

/// <summary>
/// Application entry point that sets up dependency injection and initializes the main window.
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            _host = CreateHost();
            await _host.StartAsync();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow?.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Application startup failed: {ex.Message}", "Startup Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            if (_host != null)
            {
                await _host.StopAsync(TimeSpan.FromSeconds(5));
                _host.Dispose();
            }
        }
        catch (Exception ex)
        {
            // TODO: Consider implementing proper logging to file during shutdown
            System.Diagnostics.Debug.WriteLine($"Error during shutdown: {ex.Message}");
        }

        base.OnExit(e);
    }

    private static IHost CreateHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<IJsonFileService, JsonFileService>();
                services.AddSingleton<IFileMonitoringService, FileMonitoringService>();

                services.AddTransient<MainViewModel>();

                services.AddTransient<MainWindow>();

                services.AddLogging(builder =>
                {
                    builder.AddDebug();
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Debug);
                });
            })
            .Build();
    }
}