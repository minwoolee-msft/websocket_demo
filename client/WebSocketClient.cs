using System.Net.WebSockets;
using System.Text;

namespace client
{
    public class WebSocketClient
    {
        public volatile bool isConnected = false;

        public async Task TryToEstablishWebsocketConnection(string WebSocketUrl, string payload)
        {
            using (var client = new ClientWebSocket())
            {
                try
                {
                    await client.ConnectAsync(new Uri(WebSocketUrl), CancellationToken.None);
                    await SendPayload(client, payload);
                    await ReceiveResponses(client);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"WebSocket error: {ex.Message}");
                }
            }
        }

        private async Task SendPayload(ClientWebSocket client, string payload)
        {
            var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(payload));
            await client.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task ReceiveResponses(ClientWebSocket client)
        {
            var buffer = new byte[8192];
            var stringBuilder = new StringBuilder();

            while (client.State == WebSocketState.Open)
            {
                var receiveResult = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    await client.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                }
                else
                {
                    var response = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                    stringBuilder.Append(response);

                    if (receiveResult.EndOfMessage)
                    {
                        string msg = stringBuilder.ToString();
                        stringBuilder.Clear();

                        if (msg == "ack")
                        {
                            isConnected = true;
                        }
                        Program.EventRecieved(msg);
                    }
                }
            }
        }
    }
}
