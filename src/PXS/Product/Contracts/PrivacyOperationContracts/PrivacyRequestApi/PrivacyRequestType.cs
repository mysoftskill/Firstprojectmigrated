// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.PrivacyOperation.Contracts
{
    /// <summary>
    ///     Public enum PrivacyRequestType
    /// </summary>
    public enum PrivacyRequestType
    {
        /// <summary>
        ///     None.
        /// </summary>
        None = 0,

        /// <summary>
        ///     Delete.
        /// </summary>
        Delete = 1,

        /// <summary>
        ///     Export.
        /// </summary>
        Export = 2,

        /// <summary>
        ///     Account close.
        /// </summary>
        AccountClose = 3
    }
}
