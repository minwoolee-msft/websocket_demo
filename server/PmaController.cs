using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json;

namespace server
{
    public class PmaController : ApiController
    {
        [HttpGet]
        public HttpResponseMessage Get()
        {
            // Handle GET request
            var data = new { Message = "Hello, World!" };
            var json = JsonConvert.SerializeObject(data);

            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            return response;
        }

        [HttpPost]
        public HttpResponseMessage CreateCall(string payload)
        {
            // This is CreateCall
            Console.WriteLine($"[SERVER] CreateCallRequest[{payload}]");

            // prepare websocket url with key
            WebSocketServer.GetWebsocketUrlWithKey(out string websocketurl, out string key);

            // this is actual thread that makes call and sends events
            Task.Run(async () =>
            {
                await CreateCallThenSendEventInAsync(payload, key);
            });

            // return createcall payload response, with websocket details
            var response = Request.CreateResponse(HttpStatusCode.OK);
            var data = new { 
                Message = "CreateCallResponse:" + payload,
                WebsocketEndpoint = websocketurl,
                WebsocketKey = key,
            };
            var json = JsonConvert.SerializeObject(data);
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            return response;
        }

        private async Task CreateCallThenSendEventInAsync(string payload, string key)
        {
            for (int i = 0; i < 10; i++)
            {
                // simulate creating a call...
                await Task.Delay(TimeSpan.FromSeconds(5));

                // send event through websocket key
                await WebSocketServer.SendEventThroughWebSocket(key, $"CallAutomationEvent-{i}-{payload}");
            }
        }
    }
}