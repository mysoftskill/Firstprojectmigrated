// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.PrivacyServices.NgpProxy.PrivacyExperienceServiceProxy
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.PrivacyAdapters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.CustomerMasterAdapter;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;

    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Not supported customer master adapter.
    /// </summary>
    public class NotSupportedCustomerMasterAdapter : ICustomerMasterAdapter
    {
        /// <inheritdoc />
        public Task<AdapterResponse<PrivacyProfile>> CreatePrivacyProfileAsync(IPxfRequestContext requestContext, PrivacyProfile content)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public Task<AdapterResponse<bool?>> GetOboPrivacyConsentSettingAsync(IPxfRequestContext requestContext)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public Task<AdapterResponse<PrivacyProfile>> GetPrivacyProfileAsync(IPxfRequestContext requestContext, string operationName = "GetPrivacyProfile")
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public Task<AdapterResponse<JObject>> GetPrivacyProfileJObjectAsync(IPxfRequestContext requestContext)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public Task<AdapterResponse<PrivacyProfile>> UpdatePrivacyProfileAsync(IPxfRequestContext requestContext, PrivacyProfile updatedContent, JObject existingProfile)
        {
            throw new NotSupportedException();
        }
    }
}
