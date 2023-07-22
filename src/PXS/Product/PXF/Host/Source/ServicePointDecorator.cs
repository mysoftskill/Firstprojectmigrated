// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyHost
{
    using System;
    using System.Net;
    using Microsoft.Membership.MemberServices.Configuration;


    /// <summary>
    /// ServicePointDecorator
    /// </summary>
    public class ServicePointDecorator : HostDecorator
    {
        private readonly IPrivacyExperienceServiceConfiguration serviceConfig;

        public ServicePointDecorator(IPrivacyExperienceServiceConfiguration serviceConfig)
        {
            this.serviceConfig = serviceConfig;
        }

        /// <summary>
        /// Executes this instance.
        /// </summary>
        /// <returns></returns>
        public override ConsoleSpecialKey? Execute()
        {
            ServicePointManager.DefaultConnectionLimit = this.serviceConfig.ServicePointConfiguration.ConnectionLimit;
            ServicePointManager.MaxServicePointIdleTime = this.serviceConfig.ServicePointConfiguration.MaxIdleTime;
            return base.Execute();
        }
    }
}