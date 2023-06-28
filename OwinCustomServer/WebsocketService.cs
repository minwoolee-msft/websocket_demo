using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace OwinCustomServer
{
    public class WebsocketService
    {
        private const int BufferSize = 8192;

        private CancellationTokenSource SocketLoopTokenSource = new CancellationTokenSource();

        public WebsocketService()
        {
            Console.WriteLine($"[SERVER] WebsocketService created");
        }

        public async Task AcceptSocketAsync(IOwinContext context)
        {
            var accept = context.Get<Action<IDictionary<string, object>, Func<IDictionary<string, object>, Task>>>("websocket.Accept");
            if (accept == null)
            {
                // Bad Request
                context.Response.StatusCode = 400;
                context.Response.Write("Not a valid websocket request");
                return;
            }
            context.Response.Headers.Set("X-Content-Type-Options", "nosniff");
            accept(null, RunWebSocket);
        }

        private async Task RunWebSocket(IDictionary<string, object> websocketContext)
        {
            object value;
            if (websocketContext.TryGetValue(typeof(WebSocketContext).FullName, out value))
            {
                var loopToken = SocketLoopTokenSource.Token;
                var webSocketContext = (WebSocketContext)value;
                WebSocket socket = webSocketContext.WebSocket;
                var buffer = new byte[BufferSize];

                while (socket.State != WebSocketState.Closed && socket.State != WebSocketState.Aborted && !SocketLoopTokenSource.IsCancellationRequested)
                {
                    var receiveResult = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), loopToken);

                    if (!loopToken.IsCancellationRequested)
                    {
                        if (socket.State == WebSocketState.CloseReceived && receiveResult.MessageType == WebSocketMessageType.Close)
                        {
                            await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Acknowledge Close frame", CancellationToken.None);
                        }

                        if (socket.State == WebSocketState.Open)
                        {
                            Console.WriteLine($"[SERVER] New Connection Opened.");
                            await Send(socket, "ack", loopToken);

                            Task.Run(() => CreateCallThenSendEventInAsync(socket, DateTime.Now.ToString()));
                        }
                    }
                }
            }
        }

        private async Task Send(WebSocket webSocket, string input, CancellationToken cancellationToken = default)
        {
            if (webSocket.State == WebSocketState.Open)
            {
                var buffer = Encoding.UTF8.GetBytes(input);
                var sendBuffer = new ArraySegment<byte>(buffer);

                await webSocket.SendAsync(sendBuffer, WebSocketMessageType.Text, true, cancellationToken);
            }
        }

        private async Task CreateCallThenSendEventInAsync(WebSocket websocket, string payload)
        {
            for (int i = 0; i < 10; i++)
            {
                // simulate creating a call...
                await Task.Delay(TimeSpan.FromSeconds(5));

                // send event through websocket key
                await Send(websocket, $"CallAutomationEvent-{i}-{payload}");
            }
        }
    }
}
