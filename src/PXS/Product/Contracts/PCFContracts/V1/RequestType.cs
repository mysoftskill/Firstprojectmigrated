// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.PrivacyServices.PXS.Command.Contracts.V1
{
    /// <summary>
    ///     Public enum RequestType
    /// </summary>
    public enum RequestType
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
        AccountClose = 3,

        /// <summary>
        ///     Age Out.
        /// </summary>
        AgeOut = 4
    }
}
