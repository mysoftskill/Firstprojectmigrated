namespace Microsoft.PrivacyServices.DataManagement.Worker.Autofac
{
    using System;
    using System.Collections.Generic;

    using global::Autofac;
    using global::Autofac.Core;
    using Microsoft.PrivacyServices.DataManagement.Common;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.AzureQueue;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Scheduler;
    using Microsoft.PrivacyServices.DataManagement.Worker.ChangeFeedReader;
    using Microsoft.PrivacyServices.DataManagement.Worker.ChangeFeedReader.Sll;
    using Microsoft.PrivacyServices.DataManagement.Worker.Scheduler;

    using Queue = Microsoft.Azure.Storage.Queue;

    /// <summary>
    /// Registers all <c>Autofac</c> modules.
    /// </summary>
    public class ChangeFeedReaderModule : Module
    {
        /// <summary>
        /// Registers dependencies for the ChangeFeedReaderWorker with <c>Autofac</c>.
        /// </summary>
        /// <param name="builder">The container builder to which it should add the registrations.</param>
        protected override void Load(ContainerBuilder builder)
        {
            IList<string> queueTypes = new List<string> {
                    "DataOwners",
                    "AssetGroups",
                    "VariantDefinitions"
            };

            Func<IComponentContext, CloudQueue> registerQueue(string src, string name) => ctx =>
               {
                   var configInfo = ctx.ResolveNamed<ICloudQueueConfig>("CloudQueueConfig");

                   var queueName = string.Format(configInfo.DataGridQueueName, name.ToLower());
                   var cloudQueue = ctx.Resolve<Queue.CloudQueueClient>().GetQueueReference(queueName);
                   var sessionFactory = ctx.Resolve<ISessionFactory>();
                   return new CloudQueue(cloudQueue, sessionFactory, configInfo, ctx.Resolve<IDateFactory>());
               };

            // Register DataGrid queues
            foreach (var queueType in queueTypes)
            {
                builder
                .Register(registerQueue("DataGrid", queueType))
                .Named<ICloudQueue>($"{queueType}Queue")
                .As<IInitializer>()
                .SingleInstance();
            }

            // Per request classes.
            builder.RegisterType<LockDataAccess<ChangeFeedReaderLockState>>().As<ILockDataAccess<ChangeFeedReaderLockState>>().InstancePerLifetimeScope();
            builder.RegisterType<ChangeFeedReader>().As<IChangeFeedReader>().InstancePerLifetimeScope();

            WorkerType workerType = WorkerType.ChangeFeedReaderWorker;
            builder
                 .RegisterType<SessionProperties>()
                 .Keyed<SessionProperties>(workerType)
                 .As<SessionProperties>()
                 .InstancePerLifetimeScope();

            builder
                .Register(c => new SessionFactory(c.Resolve<ISessionWriterFactory>(), c.Resolve<SessionProperties>()))
                .Keyed<ISessionFactory>(workerType)
                .As<ISessionFactory>()
                .AsSelf()
                .InstancePerLifetimeScope();

            Guid workerId = Guid.NewGuid();

            builder
                .RegisterType<ChangeFeedReaderWorker>()
                .Keyed<IWorker>(WorkerType.ChangeFeedReaderWorker)
                .As<IWorker>()
                .WithParameter("id", workerId)
                .WithParameter(
                    new ResolvedParameter(
                        (pi, ctx) => pi.ParameterType == typeof(ISessionFactory),
                        (pi, ctx) => ctx.ResolveKeyed<ISessionFactory>(workerType)))
                .WithParameter(
                    new ResolvedParameter(
                        (pi, ctx) => pi.ParameterType == typeof(ICloudQueue) && pi.Name == "dataOwnersQueue",
                        (pi, ctx) => ctx.ResolveNamed<ICloudQueue>("DataOwnersQueue")))
                .WithParameter(
                    new ResolvedParameter(
                        (pi, ctx) => pi.ParameterType == typeof(ICloudQueue) && pi.Name == "assetGroupsQueue",
                        (pi, ctx) => ctx.ResolveNamed<ICloudQueue>("AssetGroupsQueue")))
                .WithParameter(
                    new ResolvedParameter(
                        (pi, ctx) => pi.ParameterType == typeof(ICloudQueue) && pi.Name == "variantDefinitionsQueue",
                        (pi, ctx) => ctx.ResolveNamed<ICloudQueue>("VariantDefinitionsQueue")))
                .InstancePerLifetimeScope();

            builder
                .Register(ctx => new WorkScheduler(ctx.Resolve<ILifetimeScope>(), workerType))
                .InstancePerLifetimeScope();

            // Session writers.
            builder
                .RegisterType<LockStatusSessionWriter<ChangeFeedReaderLockState>>()
                .Keyed<LockStatusSessionWriter<ChangeFeedReaderLockState>>(workerType)
                .As<ISessionWriter<Tuple<Lock<ChangeFeedReaderLockState>, string>>>()
                .AsSelf()
                .InstancePerLifetimeScope();

            builder.RegisterType<FullSyncEventWriter>().As<IEventWriter<FullSyncTriggerEvent>>().InstancePerLifetimeScope();
            builder.RegisterType<EnqueuingEventWriter>().As<IEventWriter<EnqueuingMessageEvent>>().InstancePerLifetimeScope();
            builder.RegisterType<EnqueuedEventWriter>().As<IEventWriter<EnqueuedMessageEvent>>().InstancePerLifetimeScope();
        }
    }
}
