// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.AzureInfraCommon.IfxCommon
{
    using Microsoft.Membership.MemberServices.AzureInfraCommon.Common;
    using Microsoft.Membership.MemberServices.Configuration;

    using Cloud = Microsoft.Cloud.InstrumentationFramework;

    /// <inheritdoc />
    /// <summary>
    ///     This class handles initializing IFX so that we can emit logs, operations, metrics, etc
    ///     in cloud service environment.
    /// </summary>
    public class CloudServiceIfxInitializer : IfxInitializer
    {
        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the CloudServiceIfxInitializer class
        /// </summary>
        /// <param name="envSettings">The environment variables configuration</param>
        public CloudServiceIfxInitializer(IIfxEnvironment envSettings)
            : base(envSettings)
        {
        }

        /// <summary>
        ///     <seealso cref="IfxInitializer.NonDevEnvInitialize" />
        /// </summary>
        protected override void NonDevEnvInitialize(IIfxEnvironment envSettings, string logDirectory)
        {
            var instrumentationSpecification = new Cloud.InstrumentationSpecification()
            {
                TraceDirectory = logDirectory
            };

            Cloud.IfxInitializer.IfxInitialize(envSettings.Datacenter, envSettings.Role, envSettings.RoleInstance, instrumentationSpecification);
        }
    }
}
