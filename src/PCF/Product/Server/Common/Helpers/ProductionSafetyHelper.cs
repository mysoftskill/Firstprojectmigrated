namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;

    /// <summary>
    /// Contains a helper method to throw an exception if the current environment is production.
    /// This is a defense in depth measure, since these chunks of code *should* be conditionally compiled out,
    /// but accidents happen.
    /// </summary>
    public static class ProductionSafetyHelper
    {
        /// <summary>
        /// Throws an exception if the current environment is production.
        /// </summary>
        public static void EnsureNotInProduction()
        {
            if (Config.Instance.Common.IsProductionEnvironment)
            {
                throw new InvalidOperationException("Method was invoked when running in production. This isn't allowed!");
            }
        }
    }
}
