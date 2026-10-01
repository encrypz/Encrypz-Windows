using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Encrypz.Desktop.ViewModels;

namespace Encrypz.Desktop;

public partial class MainWindow : Window
{
    private readonly CancellationTokenSource _lifetime = new();
    private bool _starting;
    private bool _ready;
    private bool _closed;
    private readonly DispatcherTimer _serviceMonitor = new() { Interval = TimeSpan.FromSeconds(3) };

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _serviceMonitor.Tick += (_, _) =>
        {
            if (_ready && !_starting && !((App)Application.Current).IsApiRunning)
                ShowFailure("The local service has stopped. Choose Try again to reconnect, or open the logs for details.");
        };
        _serviceMonitor.Start();
        Loaded += async (_, _) => await InitializeAsync();
        Closed += (_, _) =>
        {
            _closed = true;
            _serviceMonitor.Stop();
            _lifetime.Cancel();
            webView.Dispose();
        };
    }

    private async Task InitializeAsync()
    {
        if (_starting || _closed) return;
        _starting = true;
        SettingsButton.IsEnabled = false;
        RetryButton.Visibility = Visibility.Collapsed;
        StartupProgress.Visibility = Visibility.Visible;
        StartupTitle.Text = "Opening your workspace";
        StartupDetail.Text = "Starting the local service and connecting to your database…";
        StatusLabel.Text = "Starting local service";
        try
        {
            var distFolder = Path.Combine(AppContext.BaseDirectory, "dist");
            if (!File.Exists(Path.Combine(distFolder, "index.html")))
                throw new FileNotFoundException("The application UI is missing. Reinstall Encrypz using the complete Windows package.");

            if (!File.Exists(SettingsWindow.SettingsPath) &&
                string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")))
            {
                StartupTitle.Text = "Welcome to Encrypz";
                StartupDetail.Text = "Set up your database and Google Drive connection to open your vault.";
                if (new SettingsWindow { Owner = this }.ShowDialog() != true)
                    throw new InvalidOperationException("Open Connection settings to finish setup, then choose Try again.");
            }

            await ((App)Application.Current).StartApiAsync(_lifetime.Token);
            StartupDetail.Text = "Preparing your vault…";
            var environment = await CoreWebView2Environment.CreateAsync(null,
                Path.Combine(App.DataDirectory, "WebView2"));
            if (_closed) return;
            await webView.EnsureCoreWebView2Async(environment);
            if (_closed) return;
            webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "app.encrypz.local", distFolder, CoreWebView2HostResourceAccessKind.DenyCors);
            webView.CoreWebView2.NavigationCompleted -= NavigationCompleted;
            webView.CoreWebView2.NavigationCompleted += NavigationCompleted;
            webView.DefaultBackgroundColor = System.Drawing.Color.FromArgb(10, 10, 10);
            webView.CoreWebView2.Navigate("https://app.encrypz.local/index.html");
        }
        catch (OperationCanceledException) when (_closed) { }
        catch (Exception ex)
        {
            if (!_closed)
            {
                ((App)Application.Current).StopApi();
                ShowFailure(ex.Message);
                App.Log(ex.ToString());
            }
        }
        finally
        {
            _starting = false;
            if (!_closed) SettingsButton.IsEnabled = true;
        }
    }

    private void NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (_closed) return;
        if (!e.IsSuccess)
        {
            ShowFailure($"The page could not load ({e.WebErrorStatus}). Check your connection and try again.");
            return;
        }
        _ready = true;
        webView.Visibility = Visibility.Visible;
        StartupPanel.Visibility = Visibility.Collapsed;
        StatusLabel.Text = "Encrypz Desktop • Local service running";
    }

    private void ShowFailure(string message)
    {
        _ready = false;
        webView.Visibility = Visibility.Hidden;
        StartupPanel.Visibility = Visibility.Visible;
        StartupProgress.Visibility = Visibility.Collapsed;
        StartupTitle.Text = "Let’s get you connected";
        StartupDetail.Text = message;
        RetryButton.Visibility = Visibility.Visible;
        StatusLabel.Text = "Setup needs attention";
    }

    private async void Retry_Click(object sender, RoutedEventArgs e) => await InitializeAsync();

    private async void Settings_Click(object sender, RoutedEventArgs e)
    {
        if (new SettingsWindow { Owner = this }.ShowDialog() == true)
        {
            if (!_ready) await InitializeAsync();
            else StatusLabel.Text = "Settings saved • Restart Encrypz to apply";
        }
    }

    private void Logs_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", App.DataDirectory) { UseShellExecute = true });
        }
        catch (Exception ex) { App.ReportError("Unable to open the log folder.", ex); }
    }
}
