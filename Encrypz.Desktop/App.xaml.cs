using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using Encrypz.Desktop.ViewModels;
using Encrypz.Infrastructure.Services;

namespace Encrypz.Desktop;

public partial class App : Application
{
    internal static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Encrypz");
    private Process? _apiProcess;
    private Mutex? _instanceMutex;
    private bool _ownsMutex;
    private string? _databaseFailureCode;

    internal bool IsApiRunning => _apiProcess is { HasExited: false };

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        try
        {
            _instanceMutex = new Mutex(true, "Local\\Encrypz.Desktop", out _ownsMutex);
            if (!_ownsMutex)
            {
                MessageBox.Show("Encrypz is already running.", "Encrypz");
                Shutdown();
                return;
            }
            Directory.CreateDirectory(DataDirectory);
            var window = new MainWindow(new MainViewModel());
            MainWindow = window;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            window.Show();
        }
        catch (Exception ex)
        {
            ReportError("Encrypz could not start.", ex);
            Shutdown(1);
        }
    }

    internal async Task StartApiAsync(CancellationToken cancellationToken)
    {
        StopApi();
        _databaseFailureCode = null;
        var apiPath = Path.Combine(AppContext.BaseDirectory, "api", "Encrypz.API.exe");
        if (!File.Exists(apiPath))
            throw new FileNotFoundException("The bundled API is missing. Build or reinstall the complete Windows package.", apiPath);

        var startInfo = new ProcessStartInfo(apiPath)
        {
            WorkingDirectory = Path.GetDirectoryName(apiPath)!,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.Environment["ASPNETCORE_URLS"] = "http://localhost:5207";
        startInfo.Environment["Desktop__InstanceId"] = Guid.NewGuid().ToString("N");
        _apiProcess = new Process { StartInfo = startInfo };
        _apiProcess.OutputDataReceived += (_, args) => Log(args.Data);
        _apiProcess.ErrorDataReceived += (_, args) =>
        {
            if (args.Data?.StartsWith(DatabaseConnectionFailure.Prefix, StringComparison.Ordinal) == true)
                Volatile.Write(ref _databaseFailureCode, args.Data[DatabaseConnectionFailure.Prefix.Length..].Trim());
            Log(args.Data);
        };
        _apiProcess.Start();
        _apiProcess.BeginOutputReadLine();
        _apiProcess.BeginErrorReadLine();

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var ready = false;
        var deadline = DateTime.UtcNow.AddSeconds(45);
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_apiProcess.HasExited)
            {
                // Drain redirected output so the diagnostic is available even on a fast exit.
                _apiProcess.WaitForExit();
                var code = Volatile.Read(ref _databaseFailureCode);
                throw new InvalidOperationException(code == null
                    ? "The local API could not start. Check database configuration and the startup log."
                    : DatabaseConnectionFailure.Describe(code));
            }
            try
            {
                var instance = await client.GetStringAsync("http://localhost:5207/health", cancellationToken);
                ready = instance == startInfo.Environment["Desktop__InstanceId"];
                if (ready) break;
            }
            catch (HttpRequestException) { }
            catch (TaskCanceledException) { }
            await Task.Delay(250, cancellationToken);
        }
        if (!ready) throw new TimeoutException("The local API did not become ready within 45 seconds. Check configuration and whether port 5207 is occupied.");
    }

    private static readonly object LogLock = new();
    internal static void Log(string? message)
    {
        if (message == null) return;
        try
        {
            lock (LogLock)
            {
                Directory.CreateDirectory(DataDirectory);
                var path = Path.Combine(DataDirectory, "startup.log");
                if (File.Exists(path) && new FileInfo(path).Length > 2_000_000)
                    File.Move(path, path + ".previous", true);
                File.AppendAllText(path, $"{DateTime.UtcNow:O} {message}{Environment.NewLine}");
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    internal static void ReportError(string message, Exception exception)
    {
        Log(exception.ToString());
        MessageBox.Show($"{message}\n\n{exception.Message}\n\nLog: {Path.Combine(DataDirectory, "startup.log")}",
            "Encrypz", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    internal void StopApi()
    {
        try
        {
            if (_apiProcess is { HasExited: false })
            {
                _apiProcess.Kill(entireProcessTree: true);
                _apiProcess.WaitForExit(5000);
            }
        }
        catch (InvalidOperationException) { }
        catch (System.ComponentModel.Win32Exception ex) { Log($"Unable to stop local service: {ex.Message}"); }
        finally
        {
            _apiProcess?.Dispose();
            _apiProcess = null;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        StopApi();
        try
        {
            if (_ownsMutex) _instanceMutex?.ReleaseMutex();
        }
        finally
        {
            _instanceMutex?.Dispose();
        }
        base.OnExit(e);
    }
}
