namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    using System;

    /// <summary>
    /// Indicates an exception that has been suppressed.
    /// </summary>
    public class SuppressedException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SuppressedException" /> class.
        /// </summary>
        /// <param name="name">The action that caused the exception.</param>
        /// <param name="value">The exception that is suppressed.</param>
        public SuppressedException(string name, Exception value)
        {
            this.Name = name;
            this.Value = value;
        }

        /// <summary>
        /// Gets the name to log.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the value to log.
        /// </summary>
        public Exception Value { get; private set; }
    }
}