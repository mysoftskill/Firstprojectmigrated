// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.Configuration
{
    using System;

    using Microsoft.Membership.MemberServices.Configuration;

    public class IfxEnvironment : IIfxEnvironment
    {
        public string MonitoringAgentSessionName => "Local";

        public string Cloud => "Public";

        public CloudInstanceType CloudInstance => CloudInstanceType.INT;

        public string Datacenter => "DataCenter01";

        public string LocalTraceLogDirectory => @"C:\Monitoring\logs";

        public string MonitoringAccount => "MonitoringAccount";

        public string Role => "Role01";

        public string RoleInstance => Environment.MachineName;

        public string ServiceName => "ADG.CS";
    }
}
