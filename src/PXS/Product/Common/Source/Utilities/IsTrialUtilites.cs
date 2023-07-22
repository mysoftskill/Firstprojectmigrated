// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.Utilities
{
    using System;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.Common.Azure;

    public static class IsTrialUtilities
    {        
        /// <summary>
        /// Reusable regular expression for checking for the word "trial". This is threadsafe
        /// https://msdn.microsoft.com/en-us/library/6h453d2h%28v=vs.110%29.aspx.
        /// 
        /// In performance testing, found that matching this regex matches very long worst case strings in
        /// sub-ms time. Never the less, use 5ms maximum for matching.
        /// </summary>
        private static readonly Regex TrialRegex = new Regex(
            @"\btrial\b", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(5));

        public static bool IsTrialRegexMatch(this string input, ILogger logger)
        {
            if (input == null)
            {
                return false;
            }

            try
            {
                return TrialRegex.IsMatch(input);
            }
            catch (RegexMatchTimeoutException exception)
            {
                logger.Error("TrialRegexMatch", exception, "Timeout while matching regex for 'trial' in input: {0}", input);
                return false;
            }
        }
    }
}
