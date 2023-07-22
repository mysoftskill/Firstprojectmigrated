using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CommonSchema.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.MarketReadiness;
using Microsoft.Osgs.Infra.Monitoring;
using Microsoft.Osgs.Infra.Monitoring.AspNetCore;
using Microsoft.Osgs.Infra.Monitoring.Context;
using Microsoft.Osgs.Infra.Platform;
using Microsoft.Osgs.Infra.Platform.Autopilot;
using Microsoft.Osgs.Infra.Platform.Sll;
using Microsoft.Osgs.Web.Core.Configuration;
using Microsoft.PrivacyServices.DataManagement.Client.ServiceTree;
using Microsoft.PrivacyServices.UX.Configuration;
using Microsoft.PrivacyServices.UX.Core.PdmsClient;
using Microsoft.PrivacyServices.UX.Core.PxsClient;
using Microsoft.PrivacyServices.UX.Models.Pdms;
using Microsoft.PrivacyServices.UX.Monitoring.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;
using System.Text;
using Microsoft.AspNetCore.Antiforgery.Internal;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Logging.Abstractions;

// https://microsoft.sharepoint.com/teams/CorpSTS/_layouts/15/start.aspx#/Microsoftcom%20AAD%20onboarding%20support/Home.aspx
// HTTP context: https://stackoverflow.com/a/28973242

namespace Microsoft.PrivacyServices.UX.Startup
{
    // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/startup
    public abstract partial class StartupBase : IStartup
    {
        private readonly Lazy<Version> appVersion = new Lazy<Version>(() => Assembly.GetEntryAssembly().GetName().Version);

        protected IConfigurationRoot StaticConfiguration { get; set; }

        protected ConfigurationForEnvironment EnvironmentConfiguration { get; set; }

        public StartupBase(IHostingEnvironment env)
        {
            //Loads in regex mapping from appsettings.json
            StaticConfiguration = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var appConfig = Path.Combine(env.ContentRootPath, "appconfig");

            EnvironmentConfiguration = new ConfigurationForEnvironment(configurationLocation: appConfig)
            {
                ForceLoadParallaxAssemblies = true
            };

        }

        /// <summary>
        /// This method gets called by the runtime before Configure method. Use this method to add services to the container.
        /// </summary>
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConfiguration(StaticConfiguration.GetSection("Logging"));
                loggingBuilder.AddConsole();
                loggingBuilder.AddDebug();
            });

            //The default path to store logs
            string logPath = StaticConfiguration.GetSection("Fabric_Folder_App_Log").Value;
#if DEBUG
            //Used adds the local devbox path
            if(logPath == null){
                logPath = Path.Combine(EnvironmentConfiguration.ConfigurationLocation, @"..\bin\Debug\data\logs\local");
            }
#endif
            services.AddSll(new SllConfiguration()
            {
                LogLocationPath = logPath,
                SllEnvelopeFiller = e =>
                {
                    e.SafeCloud().name = "PDMSUX";
                    e.SafeCloud().location = Environment.GetEnvironmentVariable("MONITORING_DATACENTER");
                }
            });

            var environment = ConfigureServiceFabricEnvironment();
            EnvironmentConfiguration.UseEnvironment(environment);
            services.AddSingleton<IEnvironmentInfo>(environment);

            services.AddSingleton<IParallaxConfiguration>(_ => EnvironmentConfiguration.Instance);
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.MinimumSameSitePolicy = SameSiteMode.Lax;
                options.HttpOnly = HttpOnlyPolicy.Always;
                options.Secure = CookieSecurePolicy.Always;
            });
            services.AddHsts(options =>
            {
                options.MaxAge = TimeSpan.FromDays(365);
            });
            services.AddHttpsRedirection(options =>
            {
                options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
                options.HttpsPort = 443;
            });

            // https://github.com/aspnet/Hosting/issues/793
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IApplicationBuilder, ApplicationBuilder>();
            services.AddSingleton<IInstrumentedRequestContextAccessor, InstrumentedRequestContextAccessor>();

            using (var dataFile = File.OpenRead(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), @"Data\MarketReadinessData.xml.gz")))
            {
                services.AddSingleton<IMarketReadinessData>(MarketReadinessData.LoadAsync(dataFile).Result);
            }

            services.AddSingleton(ParallaxConfig.Get<INgpLockdownConfig>("NgpLockdownConfig.ini", "INgpLockdownConfig"));
            services.AddSingleton(ParallaxConfig.Get<IMocksConfig>("MocksConfig.ini", "IMocksConfig"));

            // Enable CSRF token protection in a server farm environment without introducing dependencies on new infra
            services.AddSingleton<ISimpleKey>(new SimpleKey(new Secret(Encoding.UTF8.GetBytes("0DC89D3F-2C43-468E-BF1C-ABE9F43F090D"))));
            services.AddSingleton<IAntiforgeryTokenSerializer, SimpleAntiforgeryTokenSerializer>();
            services.AddAntiforgery(options =>
            {
                options.Cookie.Name = "PcdCsrfToken";
            });

            services
                .AddMvc(options =>
                {
                    options.Filters.Add(new InstrumentedMvcRequestFilter<IncomingServiceEvent>(new InstrumentedMvcRequestFilterConfiguration()
                    {
                        NewIncomingMvcOperationBehavior = NewIncomingMvcOperationBehavior.ReplaceOutstandingIncomingOperations,
                        ExplicitlyFinishOperation = false   //  InstrumentedRequestMiddleware will finish the operation.
                    }));
                })
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver()
                    {
                        NamingStrategy = new CamelCaseNamingStrategy() { ProcessDictionaryKeys = false }
                    };
                    options.SerializerSettings.Converters = new List<JsonConverter> { new StringEnumConverter(namingStrategy: new CamelCaseNamingStrategy()) };
                });

            AddCertificateFinder(services);
            AddAuthenticationServices(services);

            ConfigureCaching(services);

            ConfigureServiceClients(services);

            ConfigureCustomServices(services);

            return services.BuildServiceProvider();
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        public void Configure(IApplicationBuilder app)
        {
            var env = app.ApplicationServices.GetService<IHostingEnvironment>();
            var appLifetime = app.ApplicationServices.GetService<IApplicationLifetime>();
            var pdmsClient = app.ApplicationServices.GetService<IPdmsClientProvider>();
            var roleBasedAuthConfig = app.ApplicationServices.GetService<IRoleBasedAuthConfig>();

            //  Must be the very first thing this method does.
            ConfigureMonitoring(app, env);
            appLifetime.ShutdownSllRuntimeWhenApplicationStopped(app.ApplicationServices.GetRequiredService<ISllRuntimeInfo>());

            app.UseRewriter(GetUrlRewriterRules());

            //Checks to see if a request can from HTTP and redirects it to HTTPS,doesn't work because port 80 is blocked
            app.UseHttpsRedirection();
            //HTTP Strict Transport Security Protocol may cause issues in developer environment
            //The max-age is set by services.AddHsts()
            app.UseHsts();
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                await next();
            });
            app.UseCookiePolicy();

            app.UseStaticFiles();
            app.UseNodeModules(env);
            app.UseRequestLocalization();

            UseAuthenticationServices(app);

            app.AddProbe("/keepalive");

            //  Must be last.
            app.UseMvc(routes =>
            {
                //  All API calls.
                routes.MapRoute(
                    name: "search_route",
                    template: "api/search",
                    defaults: new { controller = "SearchApi", action = "Search" });
                routes.MapRoute(
                    name: "api_route",
                    template: "api/{action}/{id?}",
                    defaults: new { controller = "Api" });
                routes.MapRoute(
                    name: "manual_request_route",
                    template: "manual-request/api/{action}/{id?}",
                    defaults: new { controller = "ManualRequestApi" });
                routes.MapRoute(
                    name: "variant_route",
                    template: "variant/api/{action}/{id?}",
                    defaults: new { controller = "VariantApi" });
                routes.MapRoute(
                    name: "variant_admin_route",
                    template: "variant-admin/api/{action}/{id?}",
                    defaults: new { controller = "VariantAdminApi" });
                routes.MapRoute(
                    name: "agent_status_route",
                    template: "agent-status/api/{action}/{id?}",
                    defaults: new { controller = "RegistrationStatusApi" });
                routes.MapRoute(
                    name: "request_route",
                    template: "asset-transfer-request/api/{action}/{id?}",
                    defaults: new { controller = "AssetTransferRequestApi" });
                routes.MapRoute(
                    name: "cms_route",
                    template: "cms/api/{action}/{id?}",
                    defaults: new { controller = "CmsApi" });
                routes.MapRoute(
                    name: "healthcheck_route",
                    template: "healthcheck",
                    defaults: new { controller = "HealthCheck", action = "healthcheck" });

                //  All unrecognized API calls.
                routes.MapRoute(
                    name: "unknown_api_route",
                    template: "api/{*unknown}",
                    defaults: new { controller = "Api", action = "Unknown" });

                //  A catch-all for all URLs will bootstrap the SPA and let it do its job.
                routes.MapRoute(
                    name: "default_route",
                    template: "{*spaRoute}",
                    defaults: new { controller = "Home", action = "Index" });
            });
        }

        /// <summary>
        /// Configures service collection with customized dependency injection.
        /// </summary>
        protected abstract void ConfigureCustomServices(IServiceCollection services);

        /// <summary>
        /// Gets rules for URL rewriter middleware. These rules will apply both while hosted by IIS and Kestrel (i9n).
        /// </summary>
        protected virtual RewriteOptions GetUrlRewriterRules()
        {
            var options = new RewriteOptions();

            //  Bust cache for frontend bits.
            options.AddRewrite(@"^app/([\d\.]+)/(.*)", @"/js/$2?v=$1", skipRemainingRules: true);

            return options;
        }

        private static void ConfigureCaching(IServiceCollection services)
        {
            services.AddSingleton<IMemoryCache, MemoryCache>();
            services.AddSimpleCacheTracker();
        }

        private static void ConfigureServiceClients(IServiceCollection services)
        {
            services.AddFlighting();

            services.AddPdmsClient();
            services.AddPrivacyPolicies();
            services.AddPxsClient();
            services.AddServiceTreeClient();
            services.AddUhfClient();
        }
        /// <summary>
        /// Configures the Environment Information to be used in the Hosting Environment
        /// </summary>
        private EnvironmentInfo ConfigureServiceFabricEnvironment()
        {
            string environmentTypeRegexMap = StaticConfiguration.GetSection("EnvironmentInfo").GetValue<string>("EnvironmentRegexMap");
            string clusterName = StaticConfiguration.GetSection("Fabric_ApplicationName").Value;
            if (clusterName == null)
            {
                clusterName = "Local";
            }
            string machineName = StaticConfiguration.GetSection("COMPUTERNAME").Value;
            if (machineName == null)
            {
                machineName = StaticConfiguration.GetSection("Fabric_NodeId").Value;
                if (machineName == null)
                {
                    machineName = "Devbox";
                }
            }
            string environmentName = StaticConfiguration.GetSection("PCD_EnvironmentName").Value;
            if (environmentName == null)
            {
                environmentName = "Local";
            }

            return new EnvironmentInfo(environmentTypeRegexMap,
                machineName, clusterName, environmentName, appVersion.Value);
        }
        private void ConfigureMonitoring(IApplicationBuilder app, IHostingEnvironment env)
        {
            //  Must be the very first middleware in request pipeline.
            app.UseMiddleware<InstrumentedRequestMiddleware<IncomingServiceEvent>>(
                new InstrumentedRequestMiddlewareConfiguration("PDMSUX")
                {
                    ConfigureCorrelationVectorLifecycleManager = manager =>
                    {
                        manager.SeedCookieName = "PCD-CV";
                    },
                    OnBeforeRequest = context =>
                    {
                        context.CorrelationContext.TrySet(CorrelationContextProperty.AppId, "PDMSUX");
                        context.CorrelationContext.TrySet(CorrelationContextProperty.AppVersion, appVersion.Value.ToString());
                        context.CorrelationContext.TrySet(CorrelationContextProperty.PartnerId, "ust.manage.privacy.mscom.web");

                        return Task.CompletedTask;
                    }
                });

            //  Exception handling must be the first middleware following monitoring middleware.
            if (env.IsDevelopment())
            {
                //  This will affect monitoring accuracy, but will produce page with useful info in 
                //  development environment. Remove this, while you're working on monitoring pipeline.
                app.UseDeveloperExceptionPage();
            }
            else
            {
                //  This will handle exception and convert it to a correct HTTP response
                //  status code, which will be picked up by the instrumented request middleware.
                app.UseExceptionHandler(new ExceptionHandlerOptions
                {
                    //  Short-circuit stock exception handler.
                    //  https://github.com/aspnet/Docs/issues/6909
                    //  https://github.com/aspnet/Diagnostics/issues/400
                    ExceptionHandler = _ => Task.CompletedTask
                });
            }
        }
    }
    public interface ISimpleKey : IKey
    {
    }

    public sealed class SimpleKey : ISimpleKey
    {
        private readonly IAuthenticatedEncryptorFactory authenticatedEncryptorFactory = new CngGcmAuthenticatedEncryptorFactory(NullLoggerFactory.Instance);
        private readonly CngGcmAuthenticatedEncryptorDescriptor authenticatedEncryptorDescriptor;

        public DateTimeOffset ActivationDate => DateTimeOffset.MinValue.ToUniversalTime();

        public DateTimeOffset CreationDate => DateTimeOffset.MinValue.ToUniversalTime();

        public DateTimeOffset ExpirationDate => DateTimeOffset.MaxValue.ToUniversalTime();

        public bool IsRevoked => false;

        public Guid KeyId => Guid.Empty;

        public IAuthenticatedEncryptorDescriptor Descriptor => authenticatedEncryptorDescriptor;

        public IAuthenticatedEncryptor CreateEncryptor()
        {
            return authenticatedEncryptorFactory.CreateEncryptorInstance(this);
        }

        public SimpleKey(ISecret secret)
        {
            authenticatedEncryptorDescriptor = new CngGcmAuthenticatedEncryptorDescriptor(new CngGcmAuthenticatedEncryptorConfiguration(), secret);
        }
    }

    public sealed class SimpleDataProtector : IDataProtector
    {
        private readonly IKey masterKey;
        private readonly IAuthenticatedEncryptor authenticatedEncryptor;
        private readonly ArraySegment<byte> purposeBytes;

        public SimpleDataProtector(string purpose, IKey masterKey)
        {
            this.masterKey = masterKey ?? throw new ArgumentNullException(nameof(masterKey));

            authenticatedEncryptor = masterKey.CreateEncryptor();
            purposeBytes = new ArraySegment<byte>(Encoding.UTF8.GetBytes(purpose ?? throw new ArgumentNullException(nameof(purpose))));
        }

        public IDataProtector CreateProtector(string purpose)
        {
            return new SimpleDataProtector(purpose, masterKey);
        }

        public byte[] Protect(byte[] plaintext)
        {
            return authenticatedEncryptor.Encrypt(new ArraySegment<byte>(plaintext), purposeBytes);
        }

        public byte[] Unprotect(byte[] protectedData)
        {
            return authenticatedEncryptor.Decrypt(new ArraySegment<byte>(protectedData), purposeBytes);
        }
    }

    public sealed class SimpleAntiforgeryTokenSerializer : DefaultAntiforgeryTokenSerializer
    {
        public SimpleAntiforgeryTokenSerializer(ObjectPool<AntiforgerySerializationContext> pool, ISimpleKey simpleKey)
            : base(new SimpleDataProtector("PcdCsrf", simpleKey), pool)
        {
        }
    }

}
