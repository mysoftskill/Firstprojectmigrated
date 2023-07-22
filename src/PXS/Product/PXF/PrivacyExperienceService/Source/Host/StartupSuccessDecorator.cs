// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Host
{
    using System;
    using System.Diagnostics;

    using Microsoft.Membership.MemberServices.PrivacyHost;

    /// <inheritdoc />
    public class StartupSuccessDecorator : HostDecorator
    {
        /// <inheritdoc />
        public override ConsoleSpecialKey? Execute()
        {
            Trace.TraceInformation("Host startup success!");
            return base.Execute();
        }
    }
}
