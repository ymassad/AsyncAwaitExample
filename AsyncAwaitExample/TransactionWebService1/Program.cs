using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace TransactionWebService1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
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
    }
}
