using System;
using System.Threading.Tasks;
using System.Web.Http.SelfHost;
using System.Web.Http;

namespace server
{
    internal class Program
    {
        const string PMA_SERVER = "http://localhost:12020";
        const string WEBSOCKET_SERVER = "http://localhost:12030/";

        static async Task Main(string[] args)
        {
            // PMA web api server
            var config = new HttpSelfHostConfiguration(PMA_SERVER);
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "{controller}/{payload}",
                defaults: new { payload = RouteParameter.Optional }
            );

            var pma_server = new HttpSelfHostServer(config);
            pma_server.OpenAsync().Wait();
            Console.WriteLine($"[SERVER] PMA_SERVER started. Listening on {PMA_SERVER}");

            // the new Websocket server
            WebSocketServer.Start(WEBSOCKET_SERVER);
            Console.WriteLine("[SERVER] Press any key to exit...");
            Console.ReadKey(true);
            await WebSocketServer.StopAsync();
        }
    }
}
