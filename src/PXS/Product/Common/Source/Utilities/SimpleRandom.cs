// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.Utilities
{
    using System;

    /// <summary>
    ///     non-thread safe random number generator
    /// </summary>
    public class SimpleRandom : IRandom
    {
        private readonly Random rng = new Random();
        
        /// <summary>
        ///     gets the next random value
        /// </summary>
        /// <returns>resulting value</returns>
        public int Next()
        {
            return this.rng.Next();
        }

        /// <summary>
        ///     gets the next random value scaled to the provided range
        /// </summary>
        /// <param name="min">minimum value</param>
        /// <param name="max">maximum value</param>
        /// <returns>resulting value</returns>
        public int Next(
            int min, 
            int max)
        {
            return this.rng.Next(min, max);
        }
    }
}
