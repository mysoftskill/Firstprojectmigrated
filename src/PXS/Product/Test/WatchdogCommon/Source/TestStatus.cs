// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.WatchdogCommon
{
    public enum TestStatus
    {
        /// <summary>
        ///     The test was inconclusive.
        ///     e.g. The server version tested did not match the minimum version for the test.
        ///     e.g. Test setup failed.
        /// </summary>
        Unknown,

        /// <summary>
        ///     The test passed
        /// </summary>
        Pass,

        /// <summary>
        ///     The test failed
        /// </summary>
        Fail,

        /// <summary>
        ///     This status is for the tests which will run for long time and will take many iterations of WD
        /// </summary>
        Running
    }
}
