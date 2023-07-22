// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Test.Common
{
    /// <summary>
    /// Result containing either a user proxy ticket or an error message.
    /// </summary>
    public class UserProxyTicketResult
    {
        public string Ticket { get; set; }

        public string ErrorMessage { get; set; }

        public bool IsSuccess
        {
            get { return string.IsNullOrEmpty(ErrorMessage); }
        }
    }
}
