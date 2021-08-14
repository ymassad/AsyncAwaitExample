using System;
using System.Threading.Tasks;
using FakeClientLibrary;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace TransactionWebService2
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var asyncAwaitVersion = true;

            var baseUrl = "http://localhost:12345/" + (asyncAwaitVersion ? "AsyncAwait" : "Event");

            var host = CreateHostBuilder(args).Build();

            host.RunAsync();

            await FakeClient.Run(baseUrl);

            await host.StopAsync();

            await Task.Delay(2000);
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel(x =>
                    {
                        x.ListenAnyIP(12345);
                    });

                    webBuilder.UseStartup<Startup>();
                });

        public static TimeSpan TimeoutSpan = TimeSpan.FromSeconds(5);
    }
}
