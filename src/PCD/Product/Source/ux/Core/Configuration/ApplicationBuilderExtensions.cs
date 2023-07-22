using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Osgs.Core.Helpers;

namespace Microsoft.AspNetCore.Builder
{
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Configures and adds static file middleware to serve files directly from the node_modules
        /// folder in this project
        /// </summary>
        /// <param name="app">IApplicationBuilder instance.</param>
        /// <param name="environment">Environment configuration.</param>
        public static IApplicationBuilder UseNodeModules(this IApplicationBuilder app, IHostingEnvironment environment)
        {
            EnsureArgument.NotNull(app, nameof(app));
            EnsureArgument.NotNull(environment, nameof(environment));

            var path = Path.Combine(environment.ContentRootPath, "node_modules");
            var provider = new PhysicalFileProvider(path);

            var options = new FileServerOptions()
            {
                RequestPath = "/node_modules",
                EnableDirectoryBrowsing = false
            };
            options.StaticFileOptions.FileProvider = provider;

            return app.UseFileServer(options);
        }
    }
}
