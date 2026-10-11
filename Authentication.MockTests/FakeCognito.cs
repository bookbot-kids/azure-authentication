using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Authentication.MockTests
{
    /// <summary>
    /// A local stand-in for Cognito's /oauth2/token endpoint on a raw socket, so tests exercise the real HttpClient,
    /// Refit and connection settings, and "drop" can reset the connection the way production fails. Like Cognito it
    /// answers invalid_request, unsupported_grant_type or invalid_client to a malformed request; unlike Cognito it
    /// reads parameters from the form body only, so a token sent in the URL is noticed. The refresh token chooses
    /// the answer:
    ///   misconfigured    400 invalid_client (our request is wrong, not the user's token)
    ///   valid-*          200 with an access token
    ///   rejected         400 invalid_grant (Cognito rejecting the refresh token)
    ///   down             503
    ///   empty            200 without a body
    ///   drop             connection reset without a response (the production fault)
    ///   drop-once-*      connection reset the first time, 200 after that
    /// </summary>
    public static class FakeCognito
    {
        public static readonly string Url;
        public const string ClientId = "test-client";
        static readonly ConcurrentDictionary<string, int> calls = new();
        static readonly ConcurrentQueue<string> targets = new();

        static FakeCognito()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            Url = $"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/";
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    var client = await listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => Serve(client));
                }
            });
        }

        /// <summary>How many token requests Cognito received for this refresh token</summary>
        public static int Calls(string refreshToken) => calls.TryGetValue(refreshToken, out var n) ? n : 0;

        /// <summary>The request target (path and query) of every request received</summary>
        public static string[] Targets => targets.ToArray();

        static async Task Serve(TcpClient client)
        {
            using (client)
            {
                var stream = client.GetStream();
                var (target, body) = await ReadRequest(stream);
                targets.Enqueue(target);
                var form = HttpUtility.ParseQueryString(body);
                var token = form["refresh_token"] ?? "";
                var count = calls.AddOrUpdate(token, 1, (_, n) => n + 1);

                if (token == "drop" || (token.StartsWith("drop-once-") && count == 1))
                {
                    client.Client.LingerState = new LingerOption(true, 0); // close with RST, no response
                    return;
                }

                var (status, json) = token switch
                {
                    _ when target != "/oauth2/token" => ("404 Not Found", "{\"message\":\"not found\"}"),
                    _ when token == "" => ("400 Bad Request", "{\"error\":\"invalid_request\",\"error_description\":\"invalid_refresh_token\"}"),
                    _ when form["grant_type"] != "refresh_token" => ("400 Bad Request", "{\"error\":\"unsupported_grant_type\"}"),
                    _ when form["client_id"] != ClientId || token == "misconfigured" => ("400 Bad Request", "{\"error\":\"invalid_client\"}"),
                    "rejected" => ("400 Bad Request", "{\"error\":\"invalid_grant\"}"),
                    "down" => ("503 Service Unavailable", "{\"message\":\"service unavailable\"}"),
                    "empty" => ("200 OK", ""),
                    _ => ("200 OK", "{\"access_token\":\"access-for-" + token + "\",\"id_token\":\"id\",\"token_type\":\"Bearer\",\"expires_in\":3600}"),
                };
                var bytes = Encoding.UTF8.GetBytes(json);
                var head = Encoding.ASCII.GetBytes($"HTTP/1.1 {status}\r\nContent-Type: application/json\r\nContent-Length: {bytes.Length}\r\nConnection: close\r\n\r\n");
                await stream.WriteAsync(head);
                await stream.WriteAsync(bytes);
            }
        }

        static async Task<(string target, string body)> ReadRequest(NetworkStream stream)
        {
            var buffer = new MemoryStream();
            var chunk = new byte[4096];
            int headerEnd;
            while ((headerEnd = IndexOfHeaderEnd(buffer)) < 0)
            {
                var read = await stream.ReadAsync(chunk);
                if (read == 0) return ("", "");
                buffer.Write(chunk, 0, read);
            }

            var head = Encoding.ASCII.GetString(buffer.GetBuffer(), 0, headerEnd);
            var target = head.Split("\r\n")[0].Split(' ')[1];
            var length = 0;
            foreach (var line in head.Split("\r\n"))
            {
                if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                {
                    length = int.Parse(line.Substring("Content-Length:".Length).Trim());
                }
            }

            while (buffer.Length - (headerEnd + 4) < length)
            {
                var read = await stream.ReadAsync(chunk);
                if (read == 0) break;
                buffer.Write(chunk, 0, read);
            }

            return (target, Encoding.UTF8.GetString(buffer.GetBuffer(), headerEnd + 4, (int)buffer.Length - (headerEnd + 4)));
        }

        static int IndexOfHeaderEnd(MemoryStream buffer)
        {
            var data = buffer.GetBuffer();
            for (var i = 3; i < buffer.Length; i++)
            {
                if (data[i - 3] == '\r' && data[i - 2] == '\n' && data[i - 1] == '\r' && data[i] == '\n') return i - 3;
            }

            return -1;
        }
    }
}
