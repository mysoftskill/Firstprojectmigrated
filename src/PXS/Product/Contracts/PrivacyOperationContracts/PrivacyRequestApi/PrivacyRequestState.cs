// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.PrivacyOperation.Contracts
{
    /// <summary>
    ///     A privacy request state. Either submitted or completed, we do not fail.
    /// </summary>
    public enum PrivacyRequestState
    {
        /// <summary>
        ///     The request has been submitted, and is being or will be worked on
        /// </summary>
        Submitted,

        /// <summary>
        ///     The request is completed
        /// </summary>
        Completed
    }
}
