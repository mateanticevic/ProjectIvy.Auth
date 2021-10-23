using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Graylog;
using Serilog.Sinks.Graylog.Core.Transport;
using System;

namespace ProjectIvy.Auth
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug()
                                                  .MinimumLevel.Override(nameof(Microsoft), LogEventLevel.Information)
                                                  .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                                                  .Enrich.FromLogContext()
                                                  .WriteTo.Console()
                                                  .WriteTo.Graylog(new GraylogSinkOptions()
                                                  {
                                                      Facility = "project-ivy-auth",
                                                      HostnameOrAddress = Environment.GetEnvironmentVariable("GRAYLOG_HOST"),
                                                      Port = Convert.ToInt32(Environment.GetEnvironmentVariable("GRAYLOG_PORT")),
                                                      TransportType = TransportType.Udp
                                                  })
                                                  .WriteTo.File("./logs/log.txt",
                                                                LogEventLevel.Debug,
                                                                fileSizeLimitBytes: 1_000_000,
                                                                rollingInterval: RollingInterval.Day,
                                                                rollOnFileSizeLimit: true,
                                                                shared: true,
                                                                flushToDiskInterval: TimeSpan.FromSeconds(15))
                                                  .CreateLogger();

            try
            {
                var host = CreateHostBuilder(args).Build();

                Log.Information("Starting host...");
                host.Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly.");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}