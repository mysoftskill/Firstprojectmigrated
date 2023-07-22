// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.TemplateBuilder.Setup
{
    using System;
    using System.Reflection;

    using Microsoft.Practices.Unity;
    using Microsoft.PrivacyServices.Common.TemplateBuilder.Fragments;

    /// <summary>
    ///     sets up unity for the assembly
    /// </summary>
    public class UnitySetup
    {
        private static IUnityContainer container;

        /// <summary>
        ///     Gets the assembly container populated during initialization
        /// </summary>
        public static IUnityContainer Container
        {
            get
            {
                return UnitySetup.container ?? 
                       throw new InvalidOperationException(
                           $"Unity has not been initialized for the {Assembly.GetExecutingAssembly().FullName} assembly");
            }
        }

        /// <summary>
        ///     Registers types with Unity for the current assembly
        /// </summary>
        /// <param name="container">container to register types in</param>
        public static void RegisterAssemblyTypes(IUnityContainer container)
        {
            container.RegisterType<IFragmentFactory, FragmentFactory>(new ContainerControlledLifetimeManager());
            container.RegisterType<IFragment, VarFragment>(VarFragment.PrefixSequence, new TransientLifetimeManager());
            container.RegisterType<IFragment, ForeachFragment>(ForeachFragment.PrefixSequence, new TransientLifetimeManager());
            container.RegisterType<ITemplateParser, TemplateParser>(new ContainerControlledLifetimeManager());
            container.RegisterType<ITemplateStore, TemplateStore>(new ContainerControlledLifetimeManager());

            UnitySetup.container = container;
        }
    }
}
