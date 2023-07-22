namespace Microsoft.PrivacyServices.UX
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using CommandLine;
    using IniParser;
    using IniParser.Model;
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Server.Kestrel.Core;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.UX.Core.Security;

    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        private static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            var webhostBuilder = WebHost
                .CreateDefaultBuilder(args)
                .UseContentRoot(Directory.GetCurrentDirectory());

            var commandline = CommandLine.Parser.Default.ParseArguments<Options>(args);
            DualLogger logger = DualLogger.Instance;
            DualLogger.AddTraceListener();
            commandline.WithParsed(options =>
            {
                if (options.I9nMode)
                {
                    // Startup configuration for integration testing mode.
                    webhostBuilder = webhostBuilder.UseUrls("https://localhost:5000")
                      .ConfigureServices(services =>
                      {
                          services.AddSingleton<IStartup, Startup.I9nStartup>();
                      });
                }
                else
                {
                    // Startup configuration for PCD site.
#if DEBUG
                    if (Debugger.IsAttached)
                    {
                        ProcessStartInfo flattenerinfo = new ProcessStartInfo("uxflattener.cmd");
                        Process flattener = Process.Start(flattenerinfo);
                        flattener.WaitForExit();
                    }
#endif
                    var iniParser = new FileIniDataParser();
                    IniData iniData = iniParser.ReadFile("config-certsubjectname.ini.flattened.ini");
                    var certConfig = iniData["CertSubjectNameConfig"];
                    string subjectname = certConfig["CertSubjectName"];

                    var certificate = new CertificateFinder(logger).FindBySubjectName(subjectname);
                    webhostBuilder = webhostBuilder.ConfigureKestrel(serverOptions =>
                    {
                        serverOptions.ConfigureHttpsDefaults(listenOptions =>
                        {
                            // certificate is an X509Certificate2
                            listenOptions.ServerCertificate = certificate;
                        });
                    }).ConfigureServices(services =>
                    {
                        services.AddSingleton<IStartup, Startup.RealStartup>();
                    });

                    webhostBuilder.ConfigureKestrel(kresteloptions =>
                     {
                        // Set request header timeout to 5 seconds.
                        kresteloptions.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(5);
                     });

                }
            });

            return webhostBuilder;
        }
    }
}
