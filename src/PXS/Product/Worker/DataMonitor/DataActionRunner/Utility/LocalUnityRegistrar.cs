// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner.Utility
{
    using Microsoft.Practices.Unity;
    using Microsoft.PrivacyServices.Common.TemplateBuilder;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Actions;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Store;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Storage;

    using IActionAccessor = Microsoft.PrivacyServices.DataMonitor.DataAction.Store.IActionAccessor;

    /// <summary>
    ///     Local Unity registrar
    /// </summary>
    public class LocalUnityRegistrar : ILocalUnityRegistrar
    {
        /// <summary>
        ///     Setups the local container
        /// </summary>
        /// <param name="rootContainer">root container</param>
        /// <param name="accessor">action and template retriever</param>
        /// <returns>local container that is a child of the root</returns>
        public IUnityContainer SetupLocalContainer(
            IUnityContainer rootContainer,
            IActionLibraryAccessor accessor)
        {
            IUnityContainer local = rootContainer.CreateChildContainer();

            local.RegisterInstance<ITemplateAccessor>(accessor, new ContainerControlledLifetimeManager());
            local.RegisterInstance<IActionAccessor>(accessor, new ContainerControlledLifetimeManager());

            local.RegisterType<IActionRefStore, ActionRefStore>(new ContainerControlledLifetimeManager());
            local.RegisterType<ITemplateStore, TemplateStore>(new ContainerControlledLifetimeManager());
            local.RegisterType<IActionFactory, ActionFactory>(new ContainerControlledLifetimeManager());
            local.RegisterType<IActionStore, ActionStore>(new ContainerControlledLifetimeManager());

            return local;
        }
    }
}
