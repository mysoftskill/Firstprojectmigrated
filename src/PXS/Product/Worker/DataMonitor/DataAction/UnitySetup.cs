// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Setup
{
    using System;
    using System.Reflection;

    using Microsoft.Practices.Unity;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Actions;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Store;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Utility.Email;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Utility.Incidents;

    /// <summary>
    ///     sets up unity for the assembly
    /// </summary>
    public class UnitySetup
    {
        private static IUnityContainer container;

        /// <summary>
        ///     Gets the assembly container populated during initialization
        /// </summary>
        public static IUnityContainer Container =>
            UnitySetup.container ?? 
            throw new InvalidOperationException(
                $"Unity has not been initialized for the {Assembly.GetExecutingAssembly().FullName} assembly");

        /// <summary>
        ///     Registers types with Unity for the current assembly
        /// </summary>
        /// <param name="container">container to register types in</param>
        public static void RegisterAssemblyTypes(IUnityContainer container)
        {
            LifetimeManager Singleton() => new ContainerControlledLifetimeManager();
            LifetimeManager CreateNew() => new TransientLifetimeManager();

            container.RegisterType<IActionFactory, ActionFactory>(Singleton());

            container.RegisterType<IAction, AgentIncidentCreateAction>(AgentIncidentCreateAction.ActionType, CreateNew());
            container.RegisterType<IAction, TimeApplicabilityAction>(TimeApplicabilityAction.ActionType, CreateNew());
            container.RegisterType<IAction, ModelBuildAction>(ModelBuildAction.ActionType, CreateNew());
            container.RegisterType<IAction, ConstQueryAction>(ConstQueryAction.ActionType, CreateNew());
            container.RegisterType<IAction, KustoQueryAction>(KustoQueryAction.ActionType, CreateNew());
            container.RegisterType<IAction, EmailSendAction>(EmailSendAction.ActionType, CreateNew());
            container.RegisterType<IAction, ForeachActionSet>(ForeachActionSet.ActionType, CreateNew());
            container.RegisterType<IAction, LockActionSet>(LockActionSet.ActionType, CreateNew());
            container.RegisterType<IAction, ActionSet>(ActionSet.ActionType, CreateNew());
            
            container.RegisterType<IActionStore, ActionStore>(CreateNew());

            container.RegisterType<IIncidentCreator, IncidentCreator>(Singleton());

            container.RegisterType<IMailSender, IcpMailSender>(Singleton());
            container.RegisterType<IMailSender, IcpMailSender>(IcpMailSender.SenderType, Singleton());
            container.RegisterType<IMailSender, SmtpMailSender>(SmtpMailSender.SenderType, Singleton());

            UnitySetup.container = container;
        }
    }
}
