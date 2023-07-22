// ---------------------------------------------------------------------------
// <copyright file="ILeaseRenewerFactory.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Lease
{
    using System;

    using Microsoft.Practices.Unity;

    /// <summary>
    ///     contract for classes that can create lease renewer objects
    /// </summary>
    public class LeaseRenewerFactory : ILeaseRenewerFactory
    {
        private readonly IUnityContainer container;

        /// <summary>
        ///     Initializes a new instance of the LeaseRenewerFactory class
        /// </summary>
        /// <param name="container">container</param>
        public LeaseRenewerFactory(IUnityContainer container)
        {
            this.container = container ?? throw new ArgumentNullException(nameof(container));
        }

        /// <summary>
        ///     Creates a new lease renewer
        /// </summary>
        /// <returns>resulting value</returns>
        public ILeaseRenewer Create()
        {
            return this.container.Resolve<ILeaseRenewer>();
        }
    }
}
