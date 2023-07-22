// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.PerfClient
{
    public class RequestStatusModel
    {
        public long SuccessCount;

        public long ErrorCount;

        public long ErrorCount4xx;

        public long TotalCount => this.SuccessCount + this.ErrorCount + this.ErrorCount4xx;

        public readonly string ApiName;

        public RequestStatusModel(string apiName)
        {
            this.ApiName = apiName;
        }
    }
}