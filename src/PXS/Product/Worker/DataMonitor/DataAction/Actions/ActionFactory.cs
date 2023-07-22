// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Actions
{
    using System;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Practices.Unity;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     creates action objects
    /// </summary>
    public class ActionFactory : IActionFactory
    {
        private readonly IUnityContainer container;
        private readonly ILogger logger;

        /// <summary>
        ///     Initializes a new instance of the ActionFactory class
        /// </summary>
        /// <param name="container">container to resolve type from</param>
        /// <param name="logger">Geneva trace logger</param>
        public ActionFactory(
            IUnityContainer container,
            ILogger logger)
        {
            this.container = container ?? throw new ArgumentNullException(nameof(container));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        ///     Creates the action from an action type
        /// </summary>
        /// <param name="type">action type</param>
        /// <returns>resulting action</returns>
        public IAction Create(string type)
        {
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(type, nameof(type));

            try
            {
                return this.container.Resolve<IAction>(type);
            }
            catch (ResolutionFailedException e)
            {
                this.logger.Error(nameof(ActionFactory), $"Failed to resolve IAction<{type}>: {e}");
                return null;
            }
        }
    }
}
