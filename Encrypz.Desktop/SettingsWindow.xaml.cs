using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using MySqlConnector;
using Encrypz.Infrastructure.Services;

namespace Encrypz.Desktop;

public partial class SettingsWindow : Window
{
    internal static string SettingsPath => Path.Combine(App.DataDirectory, "settings.json");
    private JsonObject _settings = new();
    private readonly CancellationTokenSource _lifetime = new();

    public SettingsWindow()
    {
        InitializeComponent();
        Closed += (_, _) => _lifetime.Cancel();
        try
        {
            if (File.Exists(SettingsPath))
                _settings = JsonNode.Parse(File.ReadAllText(SettingsPath), documentOptions: new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                }) as JsonObject ?? throw new JsonException();
            ConnectionInput.Password = ReadValue("ConnectionStrings", "DefaultConnection");
            ClientIdInput.Text = ReadValue("GoogleDrive", "ClientId");
            ClientSecretInput.Password = ReadValue("GoogleDrive", "ClientSecret");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
        {
            SaveButton.IsEnabled = false;
            TestButton.IsEnabled = false;
            ErrorLabel.Text = "Existing settings could not be read. Correct settings.json in your Encrypz user folder before saving; it has not been changed.";
        }
    }

    private string ReadValue(string section, string key) => _settings[section]?[key]?.GetValue<string>() ?? "";

    private async void Test_Click(object sender, RoutedEventArgs e)
    {
        var input = ConnectionInput.Password.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            ErrorLabel.Text = "Enter a MySQL connection string first.";
            return;
        }
        TestButton.IsEnabled = SaveButton.IsEnabled = false;
        ConnectionInput.IsEnabled = false;
        ErrorLabel.Text = "Testing database access… No data will be changed.";
        try
        {
            var builder = new MySqlConnectionStringBuilder(input) { ConnectionTimeout = 10, Pooling = false };
            await using var connection = new MySqlConnection(builder.ConnectionString);
            await connection.OpenAsync(_lifetime.Token);
            await using var command = new MySqlCommand("SELECT 1", connection) { CommandTimeout = 10 };
            await command.ExecuteScalarAsync(_lifetime.Token);
            if (!_lifetime.IsCancellationRequested)
                ErrorLabel.Text = "Database connection succeeded. This checks the entered connection, not schema permissions or Google Drive. Save settings to apply it.";
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (ArgumentException)
        {
            if (!_lifetime.IsCancellationRequested)
                ErrorLabel.Text = "Invalid connection string. Use key=value pairs separated by semicolons.";
        }
        catch (Exception ex)
        {
            if (!_lifetime.IsCancellationRequested)
                ErrorLabel.Text = DatabaseConnectionFailure.Describe(DatabaseConnectionFailure.Classify(ex));
        }
        finally
        {
            if (!_lifetime.IsCancellationRequested)
            {
                TestButton.IsEnabled = SaveButton.IsEnabled = true;
                ConnectionInput.IsEnabled = true;
            }
        }
    }

    private JsonObject Section(string name)
    {
        if (_settings[name] is JsonObject section) return section;
        if (_settings[name] is not null) throw new JsonException();
        var result = new JsonObject();
        _settings[name] = result;
        return result;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var connection = ConnectionInput.Password.Trim();
            var clientId = ClientIdInput.Text.Trim();
            var secret = ClientSecretInput.Password.Trim();
            if (string.IsNullOrWhiteSpace(connection) || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(secret))
            {
                ErrorLabel.Text = "Enter the database connection string, Google client ID, and client secret.";
                return;
            }
            MySqlConnectionStringBuilder parsed;
            try { parsed = new MySqlConnectionStringBuilder(connection); }
            catch (ArgumentException)
            {
                ErrorLabel.Text = "The MySQL connection string is invalid. Use key=value pairs separated by semicolons.";
                return;
            }
            if (string.IsNullOrWhiteSpace(parsed.Server) || string.IsNullOrWhiteSpace(parsed.Database) || string.IsNullOrWhiteSpace(parsed.UserID))
            {
                ErrorLabel.Text = "Include Server, Database, and User in the connection string.";
                return;
            }
            Section("ConnectionStrings")["DefaultConnection"] = connection;
            var drive = Section("GoogleDrive");
            drive["ClientId"] = clientId;
            drive["ClientSecret"] = secret;
            drive["RedirectUri"] = "http://localhost:5207/api/Auth/google-callback";
            var frontend = Section("Frontend");
            var origins = (frontend["AllowedOrigins"]?.GetValue<string>() ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            frontend["AllowedOrigins"] = string.Join(",", new[] { "https://app.encrypz.local" }.Concat(origins).Distinct());
            Directory.CreateDirectory(App.DataDirectory);
            var temporary = SettingsPath + ".tmp";
            try
            {
                File.WriteAllText(temporary, _settings.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                File.Move(temporary, SettingsPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
            DialogResult = true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
        {
            ErrorLabel.Text = "Unable to save settings. Check the configuration format and access to your Encrypz user folder.";
        }
    }
}
