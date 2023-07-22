// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.Logging
{
    /// <summary>
    ///     IMachineIdRetriever
    /// </summary>
    public interface IMachineIdRetriever
    {
        /// <summary>
        ///     Reads the machine id
        /// </summary>
        /// <returns>The machine id</returns>
        string ReadMachineId();
    }
}
