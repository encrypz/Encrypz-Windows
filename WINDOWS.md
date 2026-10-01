# Windows build and configuration

This is a development build, not a production-ready secure vault. It retains the existing Google Drive storage and MySQL metadata database.

## Build

On Windows x64, install a .NET SDK capable of reading `.slnx`, Node.js with npm, and Microsoft Edge WebView2 Runtime. Run `powershell -ExecutionPolicy Bypass -File .\build-windows.ps1`. Add `-Installer` if Inno Setup 6 is installed. Each build creates a fresh folder under `artifacts`; the complete folder is required. .NET is bundled. WebView2 Runtime must already be installed.

Launch `Encrypz.Desktop.exe` in the new package. A native startup screen appears immediately, with connection settings, progress, retry, and access to logs. Generated packages live under `artifacts`; keep the complete package folder together.

## Configure

On first launch, enter your MySQL connection string and Google OAuth credentials in the Connection settings dialog. Register `http://localhost:5207/api/Auth/google-callback` as the Google OAuth callback. The dialog validates connection-string syntax; the local service checks database access during startup. Settings are saved atomically to `%LOCALAPPDATA%\Encrypz\settings.json`, preserving other JSON properties. Secrets in that file are stored as plain text, so protect access to your Windows account. Environment variables override settings. Changes made while the vault is open apply after restarting Encrypz.

Alternatively, copy `desktop-settings.example.json` to that path and replace placeholders locally. Never commit credentials. Logs and the WebView2 profile are stored in `%LOCALAPPDATA%\Encrypz`. The API must bind port 5207; startup reports failures with recovery controls. Closing the window cancels startup and stops the bundled service. If the service exits while the app is open, the recovery screen offers a retry. A retry reloads the vault, so unsaved UI work is lost.

A database credential was present in the original source and may remain in Git history and old binaries. Rotate that credential before using the database.

## Database connection troubleshooting

Open **Connection settings → Test database**. This opens the entered connection and runs `SELECT 1` without changing data. It does not verify schema creation permissions or Google Drive access. Save successful settings and retry startup; environment variables still override the saved configuration.

The desktop now displays specific database failures (access denied, missing database, TLS failure, connection reset, DNS failure, refusal, or timeout) instead of only reporting that the API exited.

If the connection is forcibly closed, verify that the cluster is active and copy the host and port from its current Connect panel. Check the provider's network access rules and test another network if necessary. A successful TCP connection alone does not confirm TLS or database access. Keep TLS enabled; forcing another TLS version is not a fix when both versions fail.

Run the diagnostic regression checks with `dotnet run --project tests/Encrypz.ConnectionTests -c Release`.

## Release blockers

- Login currently identifies an account by username without password authentication. API operations do not enforce account ownership.
- Google OAuth currently uses a user ID as state, and navigates within WebView2. Implement protected state and the system-browser OAuth flow before release.
- File contents use client-side encryption, but filenames are currently Base64 encoded, not encrypted; master passwords remain in sessionStorage.
- Folder recycle-bin APIs are incomplete, and automatic cleanup can permanently remove metadata after cloud deletion errors.
- A real database and Google account are needed to validate registration, authorization, upload/download round trips, restore, and persistence across restart.

Do not distribute this development build as a secure production vault until these blockers are addressed and tested.
