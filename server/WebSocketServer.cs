using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace server
{
    public static class WebSocketServer
    {
        private const int BufferSize = 8192;

        private static HttpListener Listener;

        private static CancellationTokenSource SocketLoopTokenSource;
        private static CancellationTokenSource ListenerLoopTokenSource;

        private static bool ServerIsRunning = true;

        private static string serverUrl;

        private static ConcurrentDictionary<string, WebSocket> connectedWebsockets = new ConcurrentDictionary<string, WebSocket>();

        public static void Start(string uriPrefix)
        {
            serverUrl= uriPrefix;
            SocketLoopTokenSource = new CancellationTokenSource();
            ListenerLoopTokenSource = new CancellationTokenSource();
            Listener = new HttpListener();
            Listener.Prefixes.Add(uriPrefix);
            Listener.Start();
            if (Listener.IsListening)
            {
                Console.WriteLine($"[SERVER] websocket listening: {uriPrefix}");
                Task.Run(() => ListenerProcessingLoopAsync().ConfigureAwait(false));
            }
            else
            {
                Console.WriteLine("[SERVER] failed to start.");
            }
        }

        public static async Task StopAsync()
        {
            if (Listener?.IsListening ?? false && ServerIsRunning)
            {
                Console.WriteLine("[SERVER] stopping.");

                ServerIsRunning = false;
                await CloseAllSocketsAsync(); 
                ListenerLoopTokenSource.Cancel();
                Listener.Stop();
                Listener.Close();
            }
        }

        public static void GetWebsocketUrlWithKey(out string websocketurl, out string key)
        {
            websocketurl = serverUrl.Replace("http://", "ws://").Replace("https://", "wss://");
            key = Guid.NewGuid().ToString();
            connectedWebsockets.TryAdd(key, null);
        }

        private static async Task ListenerProcessingLoopAsync()
        {
            var cancellationToken = ListenerLoopTokenSource.Token;
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    HttpListenerContext context = await Listener.GetContextAsync();
                    if (ServerIsRunning)
                    {
                        if (context.Request.IsWebSocketRequest)
                        {
                            HttpListenerWebSocketContext wsContext = null;
                            try
                            {
                                wsContext = await context.AcceptWebSocketAsync(subProtocol: null);
                                _ = Task.Run(() => SocketProcessingLoopAsync(wsContext.WebSocket).ConfigureAwait(false));
                            }
                            catch (Exception)
                            {
                                context.Response.StatusCode = 500;
                                context.Response.StatusDescription = "WebSocket upgrade failed";
                                context.Response.Close();
                                return;
                            }
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 409;
                        context.Response.StatusDescription = "Server is shutting down";
                        context.Response.Close();
                        return;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task SendEventThroughWebSocket(string Key, string eventInput, CancellationToken cancellationToken = default)
        {
            if (connectedWebsockets.TryGetValue(Key, out var webSocket))
            {
                if (webSocket != null)
                {
                    await Send(webSocket, eventInput, cancellationToken);
                }
            }
        }

        private static async Task SocketProcessingLoopAsync(WebSocket socket)
        {
            string key = string.Empty;
            var loopToken = SocketLoopTokenSource.Token;
            try
            {
                var buffer = new byte[BufferSize];
                while (socket.State != WebSocketState.Closed && socket.State != WebSocketState.Aborted && !loopToken.IsCancellationRequested)
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
                            key = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);

                            if (connectedWebsockets.ContainsKey(key))
                            {
                                Console.WriteLine($"[SERVER] Received existing key: {key}");
                                connectedWebsockets[key] = socket;
                                await Send(socket, "ack", loopToken);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (socket.State != WebSocketState.Closed)
                    socket.Abort();

                if (connectedWebsockets.TryRemove(key, out _))
                    socket.Dispose();
            }
        }

        private static async Task Send(WebSocket webSocket, string input, CancellationToken cancellationToken = default)
        {
            if (webSocket.State == WebSocketState.Open)
            {
                var buffer = Encoding.UTF8.GetBytes(input);
                var sendBuffer = new ArraySegment<byte>(buffer);

                await webSocket.SendAsync(sendBuffer, WebSocketMessageType.Text, true, cancellationToken);
            }
        }

        private static async Task CloseAllSocketsAsync()
        {
            var disposeQueue = new List<WebSocket>(connectedWebsockets.Count);

            while (connectedWebsockets.Count > 0)
            {
                var key = connectedWebsockets.ElementAt(0).Key;
                var client = connectedWebsockets.ElementAt(0).Value;

                if (client.State != WebSocketState.Open)
                {
                    Console.WriteLine($"[SERVER] socket not open, state = {client.State}");
                }
                else
                {
                    var timeout = new CancellationTokenSource(5000);
                    try
                    {
                        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", timeout.Token);
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }

                if (connectedWebsockets.TryRemove(key, out _))
                {
                    disposeQueue.Add(client);
                }

                Console.WriteLine("[SERVER] done");
            }

            SocketLoopTokenSource.Cancel();

            foreach (var socket in disposeQueue)
                socket.Dispose();
        }

    }
}
