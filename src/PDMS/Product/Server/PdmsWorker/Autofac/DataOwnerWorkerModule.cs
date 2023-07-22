namespace Microsoft.PrivacyServices.DataManagement.Worker.Autofac
{
    using global::Autofac;
    using global::Autofac.Core;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Scheduler;
    using Microsoft.PrivacyServices.DataManagement.Worker.DataOwnerWorker;
    using Microsoft.PrivacyServices.DataManagement.Worker.DataOwner;
    using Microsoft.PrivacyServices.DataManagement.Worker.Scheduler;
    using System;

    /// <summary>
    /// Registers all <c>Autofac</c> modules.
    /// </summary>
    public class DataOwnerWorkerModule : Module
    {
        /// <summary>
        /// Registers dependencies for the dataOwner worker with <c>Autofac</c>.
        /// </summary>
        /// <param name="builder">The container builder to which it should add the registrations.</param>
        protected override void Load(ContainerBuilder builder)
        {
            RegisterDataOwnerWorker(builder);
        }

        // Register the DataOwner worker
        private void RegisterDataOwnerWorker(ContainerBuilder builder)
        {
            WorkerType workerType = WorkerType.DataOwnerWorker;
            Guid workerId = Guid.NewGuid();

            builder
                .RegisterType<LockDataAccess<DataOwnerWorkerLockState>>()
                .Keyed<ILockDataAccess<DataOwnerWorkerLockState>>(workerType)
                .As<ILockDataAccess<DataOwnerWorkerLockState>>()
                .AsSelf()
                .InstancePerLifetimeScope();

            builder
                .Register(c => new SessionProperties(c.Resolve<ICorrelationVector>(), "ServiceTree", "DataOwner"))
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
                .RegisterType<DataOwnerWorker>()
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
                        (pi, ctx) => pi.ParameterType == (typeof(ILockDataAccess<DataOwnerWorkerLockState>)),
                        (pi, ctx) => ctx.ResolveKeyed<ILockDataAccess<DataOwnerWorkerLockState>>(workerType)))
                .InstancePerLifetimeScope();

            builder
                .Register(ctx => new WorkScheduler(ctx.Resolve<ILifetimeScope>(), workerType))
                .InstancePerLifetimeScope();

            builder
                .RegisterType<LockStatusSessionWriter<DataOwnerWorkerLockState>>()
                .Keyed<LockStatusSessionWriter<DataOwnerWorkerLockState>>(workerType)
                .As<ISessionWriter<Tuple<Lock<DataOwnerWorkerLockState>, string>>>()
                .InstancePerLifetimeScope();
        }
    }
}
