// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.Configuration.SovereignCloud
{
    using System;

    using Microsoft.Membership.MemberServices.Configuration;

    /// <summary>
    ///     DevIfxEnvironmentConfiguration
    /// </summary>
    public class DevIfxEnvironmentConfiguration : IIfxEnvironment
    {
        public string MonitoringAgentSessionName => "Local";

        public string Cloud => "Public";

        public CloudInstanceType CloudInstance => CloudInstanceType.INT;

        public string Datacenter => "Fake Data Center 01";

        public string LocalTraceLogDirectory => @"C:\Monitoring\logs";

        public string MonitoringAccount => "MeePxsNatCloudTest";

        public string Role => "PROXY.SERVICE";

        public string RoleInstance => Environment.MachineName;

        public string ServiceName => "NGPPROXY";
    }
}
