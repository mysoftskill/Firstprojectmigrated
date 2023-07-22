// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.AzureInfraCommon.Common
{
    using System;

    /// <summary>
    ///     Exposes methods for application to manipulate Correlation Context.
    /// </summary>
    public interface ICorrelationContext
    {
        /// <summary>
        ///     Gets the current correlation context as string
        /// </summary>
        /// <returns>A string containing the current correlation context</returns>
        string GetString();

        /// <summary>
        ///     Gets the current correlation context as byte array
        /// </summary>
        /// <returns>An byte array containing the current correlation context</returns>
        byte[] GetBytes();

        /// <summary>
        ///     Retrieves the current activity Id, which is just one piece of the correlation vector
        /// </summary>
        /// <returns>The current activity Id, which might be an empty Guid</returns>
        Guid GetActivityId();

        /// <summary>
        ///     Sets the current correlation context
        /// </summary>
        /// <param name="correlationContext">A string containing the correlation context</param>
        void Set(string correlationContext);

        /// <summary>
        ///     Sets the current correlation context
        /// </summary>
        /// <param name="correlationContext">A byte array containing the correlation context</param>
        void Set(byte[] correlationContext);

        /// <summary>
        ///     Sets the current activity Id for the existing context
        /// </summary>
        /// <param name="activityId">The new activity Id to use</param>
        void SetActivityId(Guid activityId);
    }
}
