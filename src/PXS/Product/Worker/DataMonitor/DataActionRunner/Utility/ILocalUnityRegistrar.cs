// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner.Utility
{
    using Microsoft.Practices.Unity;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Storage;

    /// <summary>
    ///     contract for objects that can register local overrides for actions
    /// </summary>
    public interface ILocalUnityRegistrar
    {
        /// <summary>
        ///     Setups the local container
        /// </summary>
        /// <param name="rootContainer">root container</param>
        /// <param name="accessor">action and template retriever</param>
        /// <returns>local container that is a child of the root</returns>
        IUnityContainer SetupLocalContainer(
            IUnityContainer rootContainer,
            IActionLibraryAccessor accessor);
    }
}
