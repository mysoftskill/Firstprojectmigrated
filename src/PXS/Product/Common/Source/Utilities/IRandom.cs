// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.Utilities
{
    /// <summary>
    ///     contract for objects implementing a random number generator 
    /// </summary>
    public interface IRandom
    {
        /// <summary>
        ///     gets the next random value
        /// </summary>
        /// <returns>resulting value</returns>
        int Next();

        /// <summary>
        ///     gets the next random value scaled to the provided range
        /// </summary>
        /// <param name="min">minimum value</param>
        /// <param name="max">maximum value</param>
        /// <returns>resulting value</returns>
        int Next(
            int min,
            int max);
    }
}
