// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Utilities
{
    using System;
    using System.Threading.Tasks;

    public static class PollingUtility
    {
        /// <summary>
        ///     Polls using the condition provided, as a lambda expression or anonymous method, until
        ///     (a) the condition is met or (b) the max retry count is surpassed.
        /// </summary>
        /// <param name="iterationWait">The iteration wait per polling cycle.</param>
        /// <param name="maximumPollRetry">The maximum poll iteration retry.</param>
        /// <param name="conditionCheck">The condition check expression; performed per iteration.</param>
        /// <example>
        ///     Usage:
        ///     PollForCondition(TimeSpan.FromSeconds(1), 5, () =&gt; SomeDataAccessObject.RetrieveCount("foo") == 1);
        ///     PollForCondition(TimeSpan.FromSeconds(1), 5, delegate() { return SomeDataAccessObject.RetrieveCount("foo") == 1; });
        /// </example>
        /// <returns><c>true</c> if condition was met; else <c>false</c>.</returns>
        public static async Task<bool> PollForCondition(
            TimeSpan iterationWait,
            int maximumPollRetry,
            Func<Task<bool>> conditionCheck)
        {
            // Init return: condition check invalid.
            bool conditionValid = false;

            do
            {
                conditionValid = await conditionCheck().ConfigureAwait(false);

                // Determine if condition met.
                if (conditionValid == false)
                {
                    // Iteration wait before beginning next polling cycle.
                    await Task.Delay(iterationWait).ConfigureAwait(false);
                    maximumPollRetry--;
                }
            } while (maximumPollRetry > 0 && conditionValid == false);

            return conditionValid;
        }
    }
}
