using System.Text.Json;

namespace client
{
    internal class Program
    {
        const string PMA_SERVER = "http://localhost:12020/pma";
        const string OWIN_WEBSOCKET = "ws://localhost:8080/ws";

        // CONTOSO CODE
        static async Task Main(string[] args)
        {
            while (true)
            {
                string payload = DateTime.Now.ToString();
                Console.WriteLine($"[CLIENT] Sending Request:{payload}");

                // (PMA) HTTP post to create call
                // var response = await CreateCall(PMA_SERVER, payload);

                // (OWIN WEBHOST)
                var websocketClient = new WebSocketClient();
                _ = Task.Run(async () =>
                {
                    await websocketClient.TryToEstablishWebsocketConnection(OWIN_WEBSOCKET, "msg");
                });

                //Console.WriteLine($"[CLIENT] Response:{response}");
                Console.ReadLine();
            };
        }
        
        public static void EventRecieved(string eventPayload)
        {
            // handle events
            Console.WriteLine($"[CLIENT] eventPayload:{eventPayload}");
        }

        // SDK CODE
        static async Task<string> CreateCall(string pmaUrl, string payload)
        {
            // HTTP put to openWebsocket for this create call
            using (HttpClient client = new HttpClient())
            {
                // send actual createcall request
                string url = $"{pmaUrl}/{payload}";
                var response = await client.PostAsync(url, null);

                response.EnsureSuccessStatusCode();

                string jsonResponse = await response.Content.ReadAsStringAsync();

                Dictionary<string, string>? parsedResponse = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonResponse);

                string message = parsedResponse["Message"];
                string websocketUrl = parsedResponse["WebsocketEndpoint"]; // <--- new response variable
                string key = parsedResponse["WebsocketKey"]; // <-- in pma/sdk, this is call connection id

                // try establish websocket channel with given websocket url and key
                var websocketClient = new WebSocketClient();
                _ = Task.Run(async () =>
                {
                    await websocketClient.TryToEstablishWebsocketConnection(websocketUrl, key);
                });

                // wait for first ack from server
                // obviously this could be better done with event handling
                while (!websocketClient.isConnected)
                {
                    await Task.Delay(100);
                }

                return message;
            }
        }
    }
}