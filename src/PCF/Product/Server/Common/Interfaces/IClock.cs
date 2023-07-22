// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;

    public interface IClock
    {
        /// <summary>Gets the current UTC time</summary>
        DateTimeOffset UtcNow { get; }
    }
}
