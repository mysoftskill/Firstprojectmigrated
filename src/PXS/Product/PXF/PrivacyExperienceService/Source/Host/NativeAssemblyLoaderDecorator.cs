// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Host
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using Microsoft.Membership.MemberServices.PrivacyHost;

    /// <summary>
    /// Native-Assembly-Loader Decorator
    /// </summary>
    public class NativeAssemblyLoaderDecorator : HostDecorator
    {
        /// <summary>
        /// Executes this instance.
        /// </summary>
        public override ConsoleSpecialKey? Execute()
        {
            Trace.TraceInformation("Begin loading native assemblies.");

            // Load the sql server native assemblies. This is used for location aggregation.
            var uriPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase);
            var path = new Uri(uriPath).LocalPath;

            Trace.TraceInformation("Attempting to load SQL native assemblies from path: {0}", path);
            SqlServerTypes.Utilities.LoadNativeAssemblies(path);

            Trace.TraceInformation("Finished loading native assemblies.");

            return base.Execute();
        }
    }
}