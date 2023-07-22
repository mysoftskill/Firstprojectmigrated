using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Osgs.Infra.Cache.Tracking;
using Microsoft.PrivacyServices.UX.Core.Cache;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class CacheExtensionsForServiceCollectionService
    {
        /// <summary>
        /// Configures simple cache tracker.
        /// </summary>
        /// <param name="services">Services collection.</param>
        public static IServiceCollection AddSimpleCacheTracker(this IServiceCollection services)
        {
            services.AddSingleton<ICacheTracking, SimpleCacheTracker>();

            return services;
        }
    }
}
