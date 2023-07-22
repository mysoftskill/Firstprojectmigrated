// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.Utilities
{
    using System;

    /// <summary>contract for a time provider</summary>
    public interface IClock
    {
        /// <summary>Gets the current UTC time</summary>
        DateTimeOffset UtcNow { get; }
    }
}
