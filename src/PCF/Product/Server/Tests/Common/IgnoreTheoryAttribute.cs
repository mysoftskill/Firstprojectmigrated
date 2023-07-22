namespace Microsoft.PrivacyServices.CommandFeed.Service.Tests.Common
{
    using System;
    using System.Globalization;
    using Xunit;

    /// <summary>
    /// Ignore test on onebox environment xunit attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class IgnoreTheoryAttribute : TheoryAttribute
    {
        /// <summary>
        /// Initializes a new Ignore
        /// </summary>
        /// <param name="ignoreOptions">The set of flags indicating when a test should be skipped.</param>
        public IgnoreTheoryAttribute(TestIgnoreOptions ignoreOptions)
        {
            if (ignoreOptions.HasFlag(TestIgnoreOptions.DevMachine) && IsRunningOnOneBox())
            {
                this.Skip = "Skipping test on dev machine.";
            }
            else if (ignoreOptions.HasFlag(TestIgnoreOptions.VSTS) && IsRunningInVSTS())
            {
                this.Skip = "Skpping in VSTS.";
            }
        }

        public static bool IsRunningOnOneBox()
        {
            return !IsRunningInVSTS();
        }

        public static bool IsRunningInVSTS()
        {
            // Pick an environment variable that VSTS defines, but isn't on our dev machine.
            string environmentVariable = Environment.GetEnvironmentVariable("BUILD_SOURCEVERSION");
            return !string.IsNullOrEmpty(environmentVariable);
        }
    }
}
