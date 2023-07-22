// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.WatchdogCommon
{
    public class TestResult
    {
        /// <summary>
        ///     Message associated with the test result.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        ///     The name of the test.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Whether or not the test passed.
        /// </summary>
        public TestStatus Status { get; set; }
    }
}
