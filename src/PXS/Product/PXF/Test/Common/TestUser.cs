// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common
{
    /// <summary>
    ///     Test User
    /// </summary>
    public class TestUser
    {
        /// <summary>
        ///     Gets or sets the password.
        /// </summary>
        public string Password { get; }

        /// <summary>
        ///     Gets or sets the puid.
        /// </summary>
        public long Puid { get; }

        /// <summary>
        ///     Gets or sets the name of the user.
        /// </summary>
        public string UserName { get; }

        public TestUser(string userName, string password)
        {
            this.UserName = userName;
            this.Password = password;
        }

        public TestUser(string userName, string password, long puid)
        {
            this.UserName = userName;
            this.Password = password;
            this.Puid = puid;
        }
    }
}
