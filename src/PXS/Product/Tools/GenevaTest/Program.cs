// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.GenevaTest
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading;

    using Microsoft.Cloud.InstrumentationFramework;
    using Microsoft.Membership.MemberServices.AzureInfraCommon;
    using Microsoft.Membership.MemberServices.AzureInfraCommon.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Practices.Unity;

    using Microsoft.PrivacyServices.Common.Azure;

    public class Program
    {
        /// <summary>
        ///     Entry point for Cosmos Export worker
        /// </summary>
        /// <param name="args">program arguments</param>
        public static void Main(string[] args)
        {
            OperationEvent();
            new Program().Run(args);
        }

        public static void OperationEvent()
        {
            IfxInitializer.IfxInitialize("IfxSession", new InstrumentationSpecification()
            {
                MonitoringAccountName = "NGPProxy-Test",
                EmitIfxMetricsEvents = true
            });
            using (Operation op = new Operation("MyOperation"))
            {
                IfxTracer.LogMessage(IfxTracingLevel.Informational, "My tag", "Hello, world!");
                op.SetResult(OperationResult.Success);
            }

            TraceManager.GetTraceOperations = eventName => new List<ITraceOperation>
            {
                new GenevaTraceOperation(eventName)
            };

            IncomingApiEventWrapper apiEvent = new IncomingApiEventWrapper { ProtocolStatusCode = "200" };

            apiEvent.Start(FrontEndApiNames.GetServiceDetailOffice);

            OutgoingApiEventWrapper outgoingApiEvent = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                Guid.NewGuid().ToString(),
                "basicoutgoingeventtest",
                "v1",
                "https://mee.privacy",
                HttpMethod.Delete,
                "outgoingtest");
            outgoingApiEvent.Start();
            outgoingApiEvent.Finish();



            apiEvent.Finish(true);
        }
        /// <summary>
        ///     Entry point for Cosmos Export worker
        /// </summary>
        /// <param name="args">program arguments</param>
        public void Run(string[] args)
        {
            IUnityContainer container = new UnityContainer();
            ICounterFactory metricFactory;
            ICounter metric1;
            ILogger logger;
            var r = new Random();

            UnitySetup.Register(container, new Config());

            metricFactory = container.Resolve<ICounterFactory>();
            logger = container.Resolve<ILogger>();

            TraceManager.GetTraceOperations = eventName => new List<ITraceOperation>
            {
                new GenevaTraceOperation(eventName)
            };

            OutgoingApiEventWrapper outgoingApiEvent2 = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                Guid.NewGuid().ToString(),
                "basicoutgoingeventtest2",
                "v1",
                "https://mee.privacy",
                HttpMethod.Delete,
                "outgoingtest");
            outgoingApiEvent2.Start();
            outgoingApiEvent2.Finish();

            IncomingApiEventWrapper apiEvent = new IncomingApiEventWrapper { ProtocolStatusCode = "200" };

            apiEvent.Start(FrontEndApiNames.GetServiceDetailOffice);

            OutgoingApiEventWrapper outgoingApiEvent = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                Guid.NewGuid().ToString(),
                "basicoutgoingeventtest",
                "v1",
                "https://mee.privacy",
                HttpMethod.Delete,
                "outgoingtest");
            outgoingApiEvent.Start();
            outgoingApiEvent.Finish();

            OutgoingCosmosDbEventWrapper outgoingCosmosDbEvent = new OutgoingCosmosDbEventWrapper("cosmosdboutgoingtest", "query", logger);
            outgoingCosmosDbEvent.Start();
            outgoingCosmosDbEvent.Finish();

            OutgoingApiEventWrapper failedOutgoingApiEvent = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                Guid.NewGuid().ToString(),
                "failedbasicoutgoingeventtest",
                "v1",
                "https://mee.privacy",
                HttpMethod.Delete,
                "outgoingtest");
            failedOutgoingApiEvent.Start();
            failedOutgoingApiEvent.PopulateFromResponseAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError }, false).Wait();
            failedOutgoingApiEvent.Finish();

            OutgoingApiEventWrapper clientErrorOutgoingApiEvent = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                Guid.NewGuid().ToString(),
                "clienterrorbasicoutgoingeventtest",
                "v1",
                "https://mee.privacy",
                HttpMethod.Delete,
                "outgoingtest");
            clientErrorOutgoingApiEvent.Start();
            clientErrorOutgoingApiEvent.PopulateFromResponseAsync(
                new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest, Content = new StringContent("invalid request") },
                false).Wait();
            clientErrorOutgoingApiEvent.Finish();

            OutgoingCosmosDbEventWrapper failedOutgoingCosmosDbEvent = new OutgoingCosmosDbEventWrapper("failedcosmosdboutgoingtest", "query", logger);
            failedOutgoingCosmosDbEvent.Start();
            failedOutgoingCosmosDbEvent.CosmosDbErrorCode = "simulateerror";
            failedOutgoingCosmosDbEvent.Finish();

            apiEvent.Finish(true);


            using (Operation operation = new Operation("Client Proxy"))
            {
                // Setting the Target endpoint for an operation automatically sets its type
                // to be ClientProxy.
                operation.TargetEndpointAddress = "https://bing";

                // Set result for the operation.
                operation.SetResult(OperationResult.Success);
            }

            logger.Error("component", "{0}", "Error log");
            logger.Warning("component", "{0}", "Warning log");
            logger.Information("component", "{0}", "Informational log");
            logger.Verbose("component", "{0}", "Verbose log");

            metric1 = metricFactory.GetCounter("MetricGroup", "M1", CounterType.None);

            for (long i = 0; i < 1000000000; ++i)
            {
                string msg;
                int all;
                int v0;
                int v1;

                v0 = r.Next(0, 100);
                v1 = r.Next(0, 100);
                all = v0 + v1;

                metric1.SetValue((ulong)all);
                metric1.SetValue((ulong)v0, "M1D1-V0-A");
                metric1.SetValue((ulong)v1, "M1D1-V1-A");

                msg = $"{i:D6} metric: {v0:D2} + {v1:D2} => {all:D3}                                        ";
                logger.Information("component", msg);
                Console.WriteLine(msg);

                logger.Error("component", "{0} + {1}", "Error log", i);
                logger.Warning("component", "{0} + {1}", "Warning log", i);
                logger.Information("component", "{0} + {1}", "Informational log", i);
                logger.Verbose("component", "{0} + {1}", "Verbose log", i);

                for (int wait = 15; wait > 0; --wait)
                {
                    Console.Write("Next metrics in " + wait.ToString("D2") + " seconds...\r");
                    Thread.Sleep(1000);
                }
            }
        }
    }

    public class Config : IIfxEnvironment
    {
        public string MonitoringAgentSessionName => "IfxSession";

        public string Cloud => "Public";


        public CloudInstanceType CloudInstance => CloudInstanceType.AzurePPE;

        public string Datacenter => "DMZ01";

        public string LocalTraceLogDirectory => @"C:\Monitoring\logs";

        public string MonitoringAccount => "NGPProxy-Test";

        public string Role => "GenevaTest";

        public string RoleInstance => "RoleInstanceValue";

        public string ServiceName => "PXS";
    }
}
