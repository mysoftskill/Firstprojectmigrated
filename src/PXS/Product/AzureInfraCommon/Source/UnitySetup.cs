// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.AzureInfraCommon
{
    using Microsoft.Membership.MemberServices.AzureInfraCommon.Common;
    using Microsoft.Membership.MemberServices.AzureInfraCommon.Logging;
    using Microsoft.Membership.MemberServices.AzureInfraCommon.Metrics;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Practices.Unity;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Regsiters assembly items for use with Unity
    /// </summary>
    public static class UnitySetup
    {
        /// <summary>
        ///      Initializes the specified container
        /// </summary>
        /// <param name="container">container to register objects in</param>
        /// <param name="config">IFx configuration</param>
        /// <returns>resulting value</returns>
        public static IUnityContainer Register(
            IUnityContainer container,
            IIfxEnvironment config)
        {
            container.RegisterInstance<IIfxEnvironment>(config);

            container.RegisterType<IIfxInitializer, IfxInitializer>(new ContainerControlledLifetimeManager());

            container.RegisterType<ILogger, IfxLogger>(new ContainerControlledLifetimeManager());

            container.RegisterType<ICorrelationContext, IfxCorrelationContext>();

            container.RegisterType<IIfxMetricInitializer, IfxMetricInitializer>(new ContainerControlledLifetimeManager());
            container.RegisterType<ICounterFactory, IfxMetricFactory>(new ContainerControlledLifetimeManager());

            return container;
        }
    }
}
