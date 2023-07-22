namespace Microsoft.PrivacyServices.AnaheimId.Config
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Builder for AId local configuration.
    /// </summary>
    public class AIdLocalConfigurationBuilder : IAIdFunctionConfigurationBuilder
    {
        private readonly List<string> configFiles;

        /// <summary>
        /// Initializes a new instance of the <see cref="AIdLocalConfigurationBuilder"/> class.
        /// </summary>
        /// <param name="configFiles">Configuration files to load.</param>
        public AIdLocalConfigurationBuilder(List<string> configFiles)
        {
            this.configFiles = configFiles;
        }

        /// <inheritdoc/>
        public IAIdFunctionConfiguration Build()
        {
            var builder = new ConfigurationBuilder();

            foreach (var file in this.configFiles)
            {
                builder.AddJsonFile(Path.Combine(Environment.CurrentDirectory, file), optional: false, reloadOnChange: true);
            }

            var root = builder.Build();
            var config = root.GetSection("Values").Get<AIdConfiguration>();

            return config;
        }
    }
}
