using System.Net.Sockets;
using System.Security.Authentication;
using MySqlConnector;

namespace Encrypz.Infrastructure.Services;

// Only known codes cross the process boundary; credentials and server errors stay out of the UI.
public static class DatabaseConnectionFailure
{
    public const string Prefix = "ENCRYPZ_DATABASE_FAILURE:";

    public static string Classify(Exception exception)
    {
        for (Exception? current = exception; current != null; current = current.InnerException)
        {
            if (current is MySqlException mysql)
            {
                if (mysql.Number is 1044 or 1045) return "access-denied";
                if (mysql.Number == 1049) return "database-missing";
            }
            if (current is AuthenticationException) return "tls";
            if (current is SocketException socket)
                return socket.SocketErrorCode switch
                {
                    SocketError.ConnectionReset => "connection-reset",
                    SocketError.HostNotFound or SocketError.NoData => "host-not-found",
                    SocketError.ConnectionRefused => "connection-refused",
                    SocketError.TimedOut => "timeout",
                    _ => "network"
                };
            if (current is TimeoutException or OperationCanceledException) return "timeout";
        }
        return "unknown";
    }

    public static string Describe(string code) => code switch
    {
        "access-denied" => "The database rejected access. Check the username, password, and database permissions in Connection settings.",
        "database-missing" => "The configured database does not exist. Check the database name and your account's permission to create it.",
        "tls" => "The database TLS handshake failed. Check the server certificate and TLS configuration. Keep encrypted connections enabled.",
        "connection-reset" => "The database connection was forcibly closed by the server or network. Check that your cloud database is active and that its Connect panel matches your host and port. A firewall or VPN may also interrupt the connection. Use Test database in Connection settings to check again.",
        "host-not-found" => "The database hostname could not be resolved. Check the server address and your internet connection.",
        "connection-refused" => "The database refused the connection. Check that the database is running and that the host, port, and network access rules are correct.",
        "timeout" => "The database connection timed out. Check your internet connection, database availability, and network access rules.",
        "network" => "The database could not be reached. Check your connection and the database network access rules.",
        _ => "The database could not be initialized. Check Connection settings and the startup log for details."
    };
}
