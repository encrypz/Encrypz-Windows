using System.Net.Sockets;
using System.Security.Authentication;
using Encrypz.Infrastructure.Services;

var cases = new (Exception Error, string Expected)[]
{
    (new InvalidOperationException("EF wrapper", new IOException("Transport error", new SocketException((int)SocketError.ConnectionReset))), "connection-reset"),
    (new SocketException((int)SocketError.HostNotFound), "host-not-found"),
    (new SocketException((int)SocketError.ConnectionRefused), "connection-refused"),
    (new SocketException((int)SocketError.TimedOut), "timeout"),
    (new SocketException((int)SocketError.NetworkUnreachable), "network"),
    (new AuthenticationException("certificate error"), "tls"),
    (new TimeoutException(), "timeout"),
    (new Exception("Password=do-not-display"), "unknown")
};
foreach (var (error, expected) in cases)
{
    var actual = DatabaseConnectionFailure.Classify(error);
    if (actual != expected) throw new Exception($"Expected {expected}; got {actual}");
    var message = DatabaseConnectionFailure.Describe(actual);
    if (string.IsNullOrWhiteSpace(message) || message.Contains("do-not-display"))
        throw new Exception("Unsafe or missing diagnostic message");
}
if (DatabaseConnectionFailure.Describe("Password=do-not-display").Contains("do-not-display"))
    throw new Exception("Unknown process output was exposed");
Console.WriteLine("PASS: 9 database failure classification and safe-message checks.");
