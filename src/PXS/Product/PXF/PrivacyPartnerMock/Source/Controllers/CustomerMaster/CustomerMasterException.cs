// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyMockService.Controllers
{
    using System;

    public class CustomerMasterException : Exception
    {
        public CustomerMasterException()
        {
        }

        public CustomerMasterException(CustomerMasterError error)
        {
            this.Error = error;
        }

        public CustomerMasterError Error { get; set; }
    }
}