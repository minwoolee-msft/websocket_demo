using System;
using System.Web.Http;
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

            // routing owin to websocket somehow
            app.UseWebApi(configuration);
        }
    }

}
