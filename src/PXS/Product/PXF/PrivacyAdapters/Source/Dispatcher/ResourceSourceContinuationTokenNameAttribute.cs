// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher
{
    using System;

    /// <summary>
    ///     Apply this attribute to all implementers of <see cref="ResourceSourceContinuationToken" /> or else
    ///     serializing your token will not work. Using this attribute to give your token type a short
    ///     name is required to keep token sizes down.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ResourceSourceContinuationTokenNameAttribute : Attribute
    {
        /// <summary>
        ///     Name of the type. Should be something extremely short, like a single character. Do not collide with others.
        /// </summary>
        public string Name { get; }

        public ResourceSourceContinuationTokenNameAttribute(string name)
        {
            this.Name = name;
        }
    }
}
