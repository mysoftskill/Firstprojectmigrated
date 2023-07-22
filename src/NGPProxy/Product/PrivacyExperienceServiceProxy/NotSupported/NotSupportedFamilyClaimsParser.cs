// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.PrivacyServices.NgpProxy.PrivacyExperienceServiceProxy
{
    using System;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;

    /// <summary>
    /// Not supported family claims parser.
    /// </summary>
    public class NotSupportedFamilyClaimsParser : IFamilyClaimsParser
    {
        /// <inheritdoc />
        public bool TryParse(string familyWebToken, out IFamilyClaims claims)
        {
            throw new NotSupportedException();
        }
    }
}
