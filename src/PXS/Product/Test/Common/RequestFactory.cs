// <copyright>
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Membership.MemberServices.Test.Common
{
    using System;
    using Microsoft.Membership.MemberServices.Common;
    
    public static class RequestFactory
    {
        private static readonly Random Random = new Random();

        /// <summary>
        /// Generates a random user PUID.
        /// </summary>
        /// <returns>Random user PUID</returns>
        public static long GeneratePuid()
        {
            return Math.Abs(GenerateLong());
        }

        /// <summary>
        /// Generates a random user CID.
        /// </summary>
        /// <returns>Random user CID</returns>
        public static long GenerateCid()
        {
            // CIDs can be negative
            return GenerateLong();
        }

        private static long GenerateLong()
        {
            byte[] buffer = new byte[8];
            Random.NextBytes(buffer);
            long value = BitConverter.ToInt64(buffer, 0);
            return value;
        }
    }
}
