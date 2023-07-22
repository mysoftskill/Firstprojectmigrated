// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models
{
    /// <summary>
    ///     Arguments for Testing MSA close.
    /// </summary>
    public class TestMsaCloseArgs : PrivacyExperienceClientBaseArgs
    {
        /// <summary>
        ///     Arguments for MSA close command for privacy subject.
        /// </summary>
        /// <param name="userProxyTicket">The user proxy ticket.</param>
        public TestMsaCloseArgs(string userProxyTicket)
            : base(userProxyTicket)
        {
        }
    }
}
