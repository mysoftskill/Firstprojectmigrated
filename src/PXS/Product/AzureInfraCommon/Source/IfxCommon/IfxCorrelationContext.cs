// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.AzureInfraCommon.Common
{
    using System;

    using Microsoft.Cloud.InstrumentationFramework;
    using Microsoft.Membership.MemberServices.Common.Logging;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Manipulates correlation context via the IFx
    /// </summary>
    public class IfxCorrelationContext : ICorrelationContext
    {
        /// <summary>
        ///     Logger to log any errors, because we don't want to throw exception
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        ///     Initializes a new instance of the IfxCorrelationContext class
        /// </summary>
        /// <param name="logger">The logger</param>
        public IfxCorrelationContext(ILogger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        ///     Gets the current correlation context as string
        /// </summary>
        /// <returns>A string containing the current correlation context </returns>
        public string GetString()
        {
            byte[] bytes = this.GetBytes();

            if (bytes != null)
            {
                return Convert.ToBase64String(bytes);
            }

            this.logger.Error(nameof(IfxCorrelationContext), "CorrelationContext.Get returns null");
            return null;
        }

        /// <summary>
        ///     Gets the current correlation context as byte array
        /// </summary>
        /// <returns>An byte array containing the current correlation context</returns>
        public byte[] GetBytes()
        {
            return CorrelationContext.Get();
        }

        /// <summary>
        ///     Retrieves the current activity Id, which is just one piece of the correlation vector
        /// </summary>
        /// <returns>The current activity Id, which might be an empty Guid</returns>
        public Guid GetActivityId()
        {
            return CorrelationContext.GetActivityId();
        }

        /// <summary>
        ///     Sets the current correlation context
        /// </summary>
        /// <param name="correlationContext">A string containing the correlation context</param>
        public void Set(string correlationContext)
        {
            if (string.IsNullOrWhiteSpace(correlationContext))
            {
                this.logger.Error(nameof(IfxCorrelationContext), "correlationContext is null/empty/whitespace");
                return;
            }

            try
            {
                this.Set(Convert.FromBase64String(correlationContext));
            }
            catch (FormatException exception)
            {
                this.logger.Error(
                    nameof(IfxCorrelationContext), 
                    "Error setting correlction vector using '{0}': {1}\n[Full stack trace: {2}]",
                    correlationContext,
                    exception.ToString(),
                    Environment.StackTrace);
            }
        }

        /// <summary>
        ///     Sets the current correlation context
        /// </summary>
        /// <param name="correlationContext">A byte array containing the correlation context</param>
        public void Set(byte[] correlationContext)
        {
            if (correlationContext == null)
            {
                this.logger.Error(nameof(IfxCorrelationContext), "correlationContext is null");
                return;
            }

            CorrelationContext.Set(correlationContext);
        }

        /// <summary>
        ///     Sets the current activity Id for the existing context
        /// </summary>
        /// <param name="activityId">The new activity Id to use</param>
        public void SetActivityId(
            Guid activityId)
        {
            CorrelationContext.SetActivityId(activityId);
        }
    }
}
