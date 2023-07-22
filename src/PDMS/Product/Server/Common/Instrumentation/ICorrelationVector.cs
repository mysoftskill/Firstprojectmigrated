namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    /// <summary>
    /// A unique identifier that is used to correlate requests across domain boundaries.
    /// </summary>
    public interface ICorrelationVector
    {
        /// <summary>
        /// Set the value for the CV or fail if a value is already set.
        /// </summary>
        /// <param name="value">The value to set.</param>
        void Set(string value);

        /// <summary>
        /// Retrieve the next instance of the CV for passing out of the current domain boundary.
        /// If no CV value was set, then initiate a new CV value and store it so that no new value can be set.
        /// </summary>
        /// <returns>The CV as a string.</returns>
        string Increment();

        /// <summary>
        /// Retrieve the current instance of the CV.
        /// If no CV value was set, then initiate a new CV value and store it so that no new value can be set.
        /// </summary>
        /// <returns>The CV as a string.</returns>
        string Get();

        /// <summary>
        /// Retrieve the raw CV value without any alteration. 
        /// This should only be used to integrate with other instrumentation libraries.
        /// </summary>
        /// <returns>The raw CV value.</returns>
        object GetRaw();
    }
}
