using System.Configuration;
using System.Data;
using System.Windows;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Encrypz.Desktop.ViewModels;

namespace Encrypz.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static IHost? AppHost { get; private set; }

    public App()
    {
        this.DispatcherUnhandledException += (s, e) => 
        {
            System.IO.File.WriteAllText("crash.log", e.Exception.ToString());
            MessageBox.Show(e.Exception.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        AppHost = Host.CreateDefaultBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                // Register ViewModels
                services.AddSingleton<MainWindow>();
                services.AddSingleton<MainViewModel>();

                // Infrastructure services would be added here
                // e.g., services.AddInfrastructure();
            })
            .Build();
    }

    private System.Diagnostics.Process? _apiProcess;

    protected override async void OnStartup(StartupEventArgs e)
    {
        await AppHost!.StartAsync();
        
        var apiPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Encrypz.API.exe");
        if (System.IO.File.Exists(apiPath))
        {
            _apiProcess = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = apiPath,
                    WorkingDirectory = System.AppDomain.CurrentDomain.BaseDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            _apiProcess.Start();
        }

        var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try 
        {
            if (_apiProcess != null && !_apiProcess.HasExited)
            {
                _apiProcess.Kill();
            }
        }
        catch { }

        await AppHost!.StopAsync();
        base.OnExit(e);
    }
}
