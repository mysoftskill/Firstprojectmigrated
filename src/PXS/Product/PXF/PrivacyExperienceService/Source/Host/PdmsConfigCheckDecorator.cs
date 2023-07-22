// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Host
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyHost;
    using Microsoft.Practices.Unity;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     PdmsConfigCheckDecorator ensures config is retrieved from PDMS prior to continuing to the next part of the decorator pipeline
    /// </summary>
    /// <remarks>This helps prevent the service from starting up without configuration from PDMS which would result in bad requests (ie there is no outbound partner to call)</remarks>
    public class PdmsConfigCheckDecorator : HostDecorator
    {
        private readonly TimeSpan deleyBetweenChecks = new TimeSpan(0, 0, 5);

        private readonly ILogger logger;

        private readonly IUnityContainer unityContainer;

        /// <summary>
        ///     Creates a new instance of PdmsConfigCheckDecorator
        /// </summary>
        /// <param name="unityContainer"></param>
        /// <param name="genevaLogger"></param>
        public PdmsConfigCheckDecorator(IUnityContainer unityContainer, ILogger genevaLogger)
        {
            this.unityContainer = unityContainer ?? throw new ArgumentNullException(nameof(unityContainer));
            this.logger = genevaLogger ?? throw new ArgumentNullException(nameof(genevaLogger));
        }

        /// <inheritdoc />
        public override ConsoleSpecialKey? Execute()
        {
            while (true)
            {
                try
                {
                    // Resolve the current instance since the instance is updated within unity whenever a new config is found.
                    var dataManagementConfig = this.unityContainer.Resolve<IDataManagementConfig>();
                    var defaultTargetRing = this.unityContainer.Resolve<IPrivacyConfigurationManager>().AdaptersConfiguration.DefaultTargetRing.ToString();

                    // Check to see if we have some config before moving on in the pipeline.
                    if (dataManagementConfig?.RingPartnerConfigMapping != null &&
                        dataManagementConfig?.RingPartnerConfigMapping.Count > 0 &&
                        dataManagementConfig.RingPartnerConfigMapping.TryGetValue(defaultTargetRing, out IRingPartnerConfigMapping ringPartnerConfigMapping) &&
                        ringPartnerConfigMapping.PartnerConfigMapping?.Count > 0)
                    {
                        var successMessage = $"PDMS configuration check passed. Configuration loaded: " +
                                             $"Default Target Ring: {defaultTargetRing}: {string.Join(", ", ringPartnerConfigMapping.PartnerConfigMapping.Keys)}";

                        this.logger.Information(nameof(PdmsConfigCheckDecorator), successMessage);
                        return base.Execute();
                    }
                }
                catch (Exception e)
                {
                    this.logger.Error(nameof(PdmsConfigCheckDecorator), e, "An unknown error occurred while checking to see if PDMS config was loaded.");
                }

                Task.Delay(this.deleyBetweenChecks).GetAwaiter().GetResult();
            }
        }
    }
}
