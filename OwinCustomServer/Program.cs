using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Contexts;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Owin;
using Microsoft.Owin.Hosting;
using Owin;

namespace OwinCustomServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string url = "http://localhost:8080";

            using (WebApp.Start<FrontEndStartup>(url))
            {
                Console.WriteLine("OWIN host started. Press Enter to exit.");
                Console.ReadLine();
            }
        }
    }

    public class FrontEndStartup
    {
        public void Configuration(IAppBuilder app)
        {
            HttpConfiguration configuration = new HttpConfiguration();

            configuration.Routes.MapHttpRoute(
                name: "HelloWorld",
                routeTemplate: "helloworld",
                defaults: new { controller = "HelloWorld" });

            // web api
            app.UseWebApi(configuration);

            // routing owin to websocket somehow
            app.Map("/ws", config => config.Use<WebsocketMiddleware>());
        }
    }

    public class WebsocketMiddleware : OwinMiddleware
    {
        WebsocketService websocketService = new WebsocketService();

        public WebsocketMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        public override Task Invoke(IOwinContext owinContext)
        {
            return websocketService.AcceptSocketAsync(owinContext);
        }
    }

}
