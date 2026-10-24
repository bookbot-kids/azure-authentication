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
    /// A local stand-in for the Sendy server's /subscribe endpoint, so tests can make Sendy fail without touching
    /// the real mailing lists. The subscriber's email chooses the answer: "down@..." gets 500, anything else 200.
    /// </summary>
    public static class FakeSendy
    {
        public static readonly string Url;
        static readonly ConcurrentDictionary<string, int> calls = new();

        static FakeSendy()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            Url = $"http://127.0.0.1:{port}/";
            var listener = new HttpListener();
            listener.Prefixes.Add(Url);
            listener.Start();
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    var context = await listener.GetContextAsync();
                    using var reader = new StreamReader(context.Request.InputStream);
                    var email = HttpUtility.ParseQueryString(await reader.ReadToEndAsync())["email"] ?? "";
                    calls.AddOrUpdate(email, 1, (_, n) => n + 1);

                    context.Response.StatusCode = email.StartsWith("down@") ? 500 : 200;
                    var body = Encoding.UTF8.GetBytes(context.Response.StatusCode == 200 ? "1" : "Internal Server Error");
                    await context.Response.OutputStream.WriteAsync(body);
                    context.Response.Close();
                }
            });
        }

        /// <summary>How many subscribe requests Sendy received for this email</summary>
        public static int Calls(string email) => calls.TryGetValue(email, out var n) ? n : 0;
    }
}
