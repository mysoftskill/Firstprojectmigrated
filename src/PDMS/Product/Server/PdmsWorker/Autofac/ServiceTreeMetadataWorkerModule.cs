using Autofac;
using Autofac.Core;
using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
using Microsoft.PrivacyServices.DataManagement.DataAccess.Scheduler;
using Microsoft.PrivacyServices.DataManagement.Worker.Scheduler;
using Microsoft.PrivacyServices.DataManagement.Worker.ServiceTreeMetadata;
using System;

namespace Microsoft.PrivacyServices.DataManagement.Worker.Autofac
{
    public class ServiceTreeMetadataWorkerModule : Module
    {
        /// <summary>
        /// Registers all <c>Autofac</c> modules.
        /// </summary>
        protected override void Load(ContainerBuilder builder)
        {
            RegisterServiceTreeMetadataWorker(builder);
        }

        // Register the ST Metadata worker
        private void RegisterServiceTreeMetadataWorker(ContainerBuilder builder)
        {
            WorkerType workerType = WorkerType.ServiceTreeMetadataWorker;
            Guid workerId = Guid.NewGuid();

            builder
                .RegisterType<LockDataAccess<ServiceTreeMetadataWorkerLockState>>()
                .Keyed<ILockDataAccess<ServiceTreeMetadataWorkerLockState>>(workerType)
                .As<ILockDataAccess<ServiceTreeMetadataWorkerLockState>>()
                .AsSelf()
                .InstancePerLifetimeScope();

            builder
                .Register(c => new SessionProperties(c.Resolve<ICorrelationVector>(), "ServiceTree", "ServiceTreeMetadata"))
                .Keyed<SessionProperties>(workerType)
                .AsSelf()
                .InstancePerLifetimeScope();

            builder
                .Register(c => new SessionWriterFactory(c.Resolve<ILifetimeScope>(), c.ResolveKeyed<SessionProperties>(workerType)))
                .Keyed<ISessionWriterFactory>(workerType)
                .As<ISessionWriterFactory>()
                .AsSelf()
                .InstancePerLifetimeScope();

            builder
                .Register(c => new SessionFactory(c.ResolveKeyed<ISessionWriterFactory>(workerType), c.ResolveKeyed<SessionProperties>(workerType)))
                .Keyed<ISessionFactory>(workerType)
                .AsSelf()
                .As<ISessionFactory>()
                .InstancePerLifetimeScope();

            builder
                .RegisterType<ServiceTreeMetadataWorker>()
                .Keyed<IWorker>(workerType)
                .As<IWorker>()
                .AsSelf()
                .WithParameter("id", workerId)
                .WithParameter(
                    new ResolvedParameter(
                        (pi, ctx) => pi.ParameterType == typeof(ISessionFactory),
                        (pi, ctx) => ctx.ResolveKeyed<ISessionFactory>(workerType)))
                .WithParameter(
                    new ResolvedParameter(
                        (pi, ctx) => pi.ParameterType == (typeof(ILockDataAccess<ServiceTreeMetadataWorkerLockState>)),
                        (pi, ctx) => ctx.ResolveKeyed<ILockDataAccess<ServiceTreeMetadataWorkerLockState>>(workerType)))
                .InstancePerLifetimeScope();

            builder
                .Register(ctx => new WorkScheduler(ctx.Resolve<ILifetimeScope>(), workerType))
                .InstancePerLifetimeScope();

            builder
                .RegisterType<LockStatusSessionWriter<ServiceTreeMetadataWorkerLockState>>()
                .Keyed<LockStatusSessionWriter<ServiceTreeMetadataWorkerLockState>>(workerType)
                .As<ISessionWriter<Tuple<Lock<ServiceTreeMetadataWorkerLockState>, string>>>()
                .InstancePerLifetimeScope();
        }
    }
}
