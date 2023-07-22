// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.PerfClient
{
    using System;

    [Flags]
    public enum UserType
    {
        None,
        View,
        Delete,
    }

    public class TestUser
    {
        public TestUser(string userName, string password, UserType userType)
        {
            this.UserName = userName;
            this.Password = password;
            this.UserType = userType;
        }

        public UserType UserType { get; private set; }

        public string UserName { get; private set; }

        public string Password { get; private set; }
    } 
}