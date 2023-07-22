// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker
{
    using System;
    using System.Diagnostics;
    using System.Net;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration.Privacy;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.Aqs;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.Host;
    using Microsoft.Membership.MemberServices.PrivacyHost;
    using Microsoft.Practices.Unity;
    
    using Microsoft.PrivacyServices.Common.Azure;

    internal static class AqsWorker
    {
        private static IDependencyManager LoadDependencies()
        {
            try
            {
                var container = new UnityContainer();
                container.RegisterInstance<ILogger>(DualLogger.Instance);
                DualLogger.AddTraceListener();

                var logger = container.Resolve<ILogger>();
                IPrivacyConfigurationLoader configLoader = new PrivacyConfigurationLoader(logger);
                var dependencyManager = new DependencyManager(container, configLoader);
                return dependencyManager;
            }
            catch (Exception e)
            {
                IfxTraceLogger.Instance.Error(nameof(AqsWorker), e, e.Message);
                throw;
            }
        }

        internal static void Main()
        {
            ResetTraceDecorator.ResetTraceListeners();
            ResetTraceDecorator.AddConsoleTraceListener();
            GenevaHelper.Initialize(TraceTagPrefixes.ADGCS_PXS);
            
            IDependencyManager dependencies = LoadDependencies();

            HostDecorator aqsDequeDecorator = new AqsDequeuerDecorator(dependencies);
            HostDecorator removeConsoleLoggerDecorator = new RemoveConsoleLoggerDecorator();

            try
            {
                IHost serviceHost = HostFactory.CreatePipeline(
                    aqsDequeDecorator,
                    removeConsoleLoggerDecorator);
                serviceHost.Execute();
            }
            catch (Exception ex)
            {
                IfxTraceLogger.Instance.Error(nameof(AqsWorker), ex, ex.Message);
                throw;
            }
            finally
            {
                var client = dependencies.Container.Resolve<IAsyncQueueService2>();
                switch(client)
                {
                    case AsyncQueueService2Client aqs2c:
                        aqs2c?.Close();
                        break;
                    case AqsRestClient aqsRestClient:
                        aqsRestClient?.Dispose();
                        break;
                }
            }
        }
    }
}
