namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    /// <summary>
    /// Enumerates the command feed services.
    /// This is the enum of the PCF services
    /// If you need to add a new service make sure it is present here.
    /// Product\Deployment\CommonSetup\ConfigureCertificates.ps1 is using this enum as a part of deployment.
    /// </summary>
    public enum CommandFeedService
    {
        /// <summary>
        /// The front door.
        /// </summary>
        Frontdoor = 0,

        /// <summary>
        /// The worker.
        /// </summary>
        Worker = 1,

        /// <summary>
        /// Unit tests.
        /// </summary>
        UnitTestService = 3,

        /// <summary>
        /// The "service" that runs to install certificates.
        /// </summary>
        CommonSetupService = 4,

        /// <summary>
        /// The frontdoor watchdog.
        /// </summary>
        FrontdoorWatchdog = 5,

        /// <summary>
        /// The worker watchdog.
        /// </summary>
        WorkerWatchdog = 6,

        /// <summary>
        /// PCF Test DataAgent
        /// </summary>
        DataAgent = 8,

        /// <summary>
        /// PCF QueueDepth service
        /// </summary>
        QueueDepth = 9,

        /// <summary>
        /// What if'er for incoming commands.
        /// </summary>
        WhatIfFilterAndRouteWorkItemHost = 10,

        /// <summary>
        /// CosmosDB auto scaler
        /// </summary>
        Autoscaler = 11,
    }
}
