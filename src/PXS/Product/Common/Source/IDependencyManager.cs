// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Common
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Practices.Unity;

    public interface IDependencyManager
    {
        /// <summary>
        ///     Gets the container.
        /// </summary>
        /// <value>
        ///     The container.
        /// </value>
        IUnityContainer Container { get; }

        /// <summary>
        ///     Gets the service.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <returns></returns>
        object GetService(Type serviceType);

        /// <summary>
        ///     Gets the services.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <returns></returns>
        IEnumerable<object> GetServices(Type serviceType);
    }
}
