// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyHost
{
    using System;
    using System.Diagnostics;
    using System.Runtime;

    /// <summary>
    /// A host startup decorator which validates app.config settings.
    /// </summary>
    public class AppConfigDecorator : HostDecorator
    {
        public override ConsoleSpecialKey? Execute()
        {
            Trace.TraceInformation("AppConfigDecorator executing");

            if (!GCSettings.IsServerGC)
            {
                throw new NotSupportedException("Enable server garbage collection in app.config");
            }

            return base.Execute();
        }
    }
}
