namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Xml;
    using System.Xml.Linq;

    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.Azure.ComplianceServices.Common.SecretClient;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.ConfigGen;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Windows.Services.ConfigGen;
    using FileMonitor = MS.Msn.Runtime.FileMonitor;
    using FileMonitorOption = MS.Msn.Runtime.FileMonitorOption;

    /// <summary>
    /// Global configuration for the Command Feed.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Lifetime object.")]
    public sealed class Config : Configuration
    {
        private const string ConfigFilenameFormat = "Config.{0}.config";

        private static readonly object SyncRoot = new object();
        private static readonly FileMonitor CommonConfigMonitor;

        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        static Config()
        {
            try
            {
                string configLoadType = "onebox";
                if (EnvironmentInfo.IsHostedEnvironment)
                {
                    configLoadType = EnvironmentInfo.EnvironmentName;
                }

                Config.CommonConfigFileName = string.Format(CultureInfo.InvariantCulture, ConfigFilenameFormat, configLoadType);
                Config.ReadConfig();
            }
            catch (Exception ex)
            {
                DualLogger.Instance.Error(nameof(Config), ex, "Fail to read config.");

                throw;
            }

            // Watch for changes.
            Config.CommonConfigMonitor = new FileMonitor(Config.CommonConfigFileName, FileMonitorOption.UseCrcFilter);
            Config.CommonConfigMonitor.FileChanged += delegate
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    try
                    {
                        Config.ReadConfig();
                        Config.RaiseConfigChanged();
                    }
                    catch (Exception ex)
                    {
                        DualLogger.Instance.Error(nameof(Config), ex, "Fail to read config.");

                        // If we get an error reading config, then just kill the process.
                        Environment.Exit(1);
                        throw;
                    }
                });
            };
        }

        private Config(XElement element, IValueParser[] parsers) : base(element, parsers)
        {
        }

        /// <summary>
        /// Gets the singleton configuration instance.
        /// </summary>
        public static Config Instance { get; private set; }

        /// <summary>
        /// Raises an event when dynamic config has changed.
        /// </summary>
        public static event EventHandler Changed;

        /// <summary>
        /// Gets the name of the common config file.
        /// </summary>
        public static string CommonConfigFileName
        {
            get;
            private set;
        }

        /// <summary>
        /// Forces a reload of the config file.
        /// </summary>
        public static void ForceReload()
        {
            ReadConfig();
        }

        /// <summary>
        /// Raises the configuration changed event.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate")]
        public static void RaiseConfigChanged()
        {
            var changed = Config.Changed;
            if (changed != null)
            {
                changed(null, EventArgs.Empty);
            }
        }

        private static void ReadConfig()
        {
            lock (Config.SyncRoot)
            {
                XDocument document = XDocument.Load(Config.CommonConfigFileName);

                // we have to do a quick load of config to discover what key vault we should talk to.
                // so we load with no KV client, then follow up with a load of the real data.
                IAzureKeyVaultClientFactory client = new NoOpKeyVaultClient();

                var config = new Config(document.Root, ConfigurationValueParsers.GetParsers(client).ToArray());
                
                client = EnvironmentInfo.HostingEnvironment.CreateKeyVaultClientFactory(
                    config.AzureKeyVault.BaseUrl,
                    config.AzureManagement.ApplicationId);
                
                config = new Config(document.Root, ConfigurationValueParsers.GetParsers(client).ToArray());

                Config.Instance = config;
            }
        }
    }
}
