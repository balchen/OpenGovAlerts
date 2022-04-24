using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace PoliticalAlertsWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IHostBuilder host = CreateHostBuilder(args);

            try
            {
                Log.Information("Starting up");
                host.Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application start-up failed");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host
                .CreateDefaultBuilder(args)
                .UseSerilog((host, configuration) => configuration.ReadFrom.Configuration(host.Configuration))
                .ConfigureWebHostDefaults(webBuilder =>
                  {
                      webBuilder
                          .UseStartup<Startup>();
                  });
        }
    }
}