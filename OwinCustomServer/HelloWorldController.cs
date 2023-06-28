using Microsoft.Owin;
using System.Net.Http;
using System.Web.Http;
using System;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.Remoting.Contexts;

namespace OwinCustomServer
{
    public class HelloWorldController : ApiController
    {
        [HttpPost]
        [Route("helloworld")]
        public IHttpActionResult GetHelloWorld()
        {
            return Ok("Hello, world!");
        }

        /*
        [HttpGet]
        public async Task<IHttpActionResult> Get()
        {
            IOwinContext owinContext = Request.GetOwinContext();
            if (IsWebSocketRequest(Request))
            {
                await websocketService.AcceptSocketAsync(owinContext).ConfigureAwait(false);
            }
            else
            {
                owinContext.Response.StatusCode = 400;
                owinContext.Response.Write("Not a valid websocket request");
                return StatusCode(HttpStatusCode.UnsupportedMediaType);
            }

            return StatusCode(HttpStatusCode.SwitchingProtocols);
            // TODO FIX: please do to not return 204
            //Task.Delay(1000000).Wait();
        }
        */

        private bool IsWebSocketRequest(HttpRequestMessage request)
        {
            if (request.Headers.TryGetValues("Upgrade", out var upgradeValues) &&
                request.Headers.TryGetValues("Connection", out var connectionValues))
            {
                var isWebSocketRequest = upgradeValues.Any(value =>
                    string.Equals(value, "websocket", StringComparison.OrdinalIgnoreCase)) &&
                    connectionValues.Any(value =>
                        string.Equals(value, "Upgrade", StringComparison.OrdinalIgnoreCase));

                return isWebSocketRequest;
            }

            return false;
        }
    }
}
