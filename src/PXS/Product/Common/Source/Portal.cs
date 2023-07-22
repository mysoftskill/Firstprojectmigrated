// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common
{
    /// <summary>
    ///     Constants for the different portals known.
    /// </summary>
    public enum Portal
    {
        /// <summary>
        ///     An unknown portal.
        /// </summary>
        Unknown = 0,

        /// <summary>
        ///     AKA account.microsoft.com
        /// </summary>
        Amc,

        /// <summary>
        ///     MSGraph
        /// </summary>
        MsGraph,

        /// <summary>
        ///     PCD (Privacy Compliance Dashboard)
        /// </summary>
        Pcd,

        /// <summary>
        ///     PXS Test Site
        /// </summary>
        PxsTest,

        /// <summary>
        ///     PXS AAD Test Site
        /// </summary>
        PxsAadTest,

        /// <summary>
        ///     Bing
        /// </summary>
        Bing
    }
}
