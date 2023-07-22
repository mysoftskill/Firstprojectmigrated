// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.ContextModelCommon.Setup
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Practices.Unity;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;

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

            AggregatingModelManipulator modelManipulator;
            JsonNetModelManipulator jsonReadWriter = new JsonNetModelManipulator();
            
            modelManipulator = new AggregatingModelManipulator(
                new Dictionary<char, IModelReader>
                {
                    { '#', new EnvironmentModelReader(container.Resolve<IClock>()) },
                    { '@', new ExtensionPropertyModelReader() }
                },
                jsonReadWriter,
                jsonReadWriter);

            container.RegisterInstance<IModelManipulator>(modelManipulator);

            container.RegisterType<IContextFactory, ContextFactory>(Singleton());

            container.RegisterType<Context>(CreateNew());
            container.RegisterType<IExecuteContext, Context>();
            container.RegisterType<IParseContext, Context>();
            container.RegisterType<IContext, Context>();

            UnitySetup.container = container;
        }
    }
}
