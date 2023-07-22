// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.CustomerMasterAdapter
{
    /// <summary>
    /// CustomerMaster ErrorCodes
    /// </summary>
    /// <remarks>
    /// These error codes originate from Customer Master and must match their values exactly.
    /// An incomplete list is found in their API spec, and undocumented ones are found in their code (speak to their devs if any error code parsing is required).
    /// https://microsoft.sharepoint.com/teams/osg_unistore/mem/mkp/Shared%20Documents/Services/Jarvis%20Customer%20Master%20service/Core%20service%20documents/Jarvis_Customer_Master_API.docx?web=1
    /// </remarks>
    public enum CustomerMasterErrorCode
    {
        Unknown,
        ConcurrencyFailure,
        UnsupportedAction,
        ResourceAlreadyExists
    }
}