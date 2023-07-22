// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.AzureInfraCommon.Common
{
    /// <summary>
    ///     Implemented by the initializer to ensure the IFX libraries are initialize before we use them. Classes that
    ///     require IFX services should add this interface as a constructor parameter. The simply act of referencing
    ///     the instance will ensure it's initialized once.
    /// </summary>
    public interface IIfxInitializer
    {
    }
}