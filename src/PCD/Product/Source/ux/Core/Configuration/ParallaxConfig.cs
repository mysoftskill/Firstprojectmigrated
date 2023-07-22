using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Osgs.Web.Core.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides access to Parallax configuration in DI scenarios.
    /// </summary>
    public static class ParallaxConfig
    {
        /// <summary>
        /// Gets an instance of the Parallax configuration object.
        /// </summary>
        /// <typeparam name="TConfig">Configuration object interface.</typeparam>
        /// <param name="configFileName">Configuration file name.</param>
        /// <param name="configSection">Configuration file section.</param>
        /// <param name="moreVariants">Optional list of additional variants.</param>
        /// <returns>A functor that will resolve configuration section with specified parameters.</returns>
        public static Func<IServiceProvider, TConfig> Get<TConfig>(string configFileName, string configSection, IEnumerable<KeyValuePair<string, string>> moreVariants = null)
             where TConfig : class
        {
            return (IServiceProvider sp) => sp.GetRequiredService<IParallaxConfiguration>().GetConfiguration<TConfig>(configFileName, configSection, moreVariants);
        }
    }
}
