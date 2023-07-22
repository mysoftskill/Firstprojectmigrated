namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    using CV = Microsoft.CommonSchema.Services.Logging;

    /// <summary>
    /// Implements the CV requirements for SLL.
    /// </summary>
    public class CorrelationVector : ICorrelationVector
    {
        /// <summary>
        /// A locking object to ensure thread safety when setting the CV.
        /// </summary>
        private readonly object lockObj = new object();

        /// <summary>
        /// The SLL object.
        /// </summary>
        private CV.CorrelationVector cv;

        /// <summary>
        /// Gets the current CV value.
        /// </summary>
        /// <returns>The CV value.</returns>
        public string Get()
        {
            return this.InitializeCV().Value;
        }

        /// <summary>
        /// Increments the CV for outbound calls.
        /// </summary>
        /// <returns>The incremented value as a string.</returns>
        public string Increment()
        {
            return this.InitializeCV().Increment();
        }

        /// <summary>
        /// Gets the raw CV object.
        /// </summary>
        /// <returns>The raw object.</returns>
        public object GetRaw()
        {
            return this.InitializeCV();
        }

        /// <summary>
        /// Sets a value for the CV.
        /// </summary>
        /// <param name="value">The value to set.</param>
        public void Set(string value)
        {
            this.InitializeCV(value);
        }

        /// <summary>
        /// Sets a value to the CV if it is not already created.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <returns>The initialized CV.</returns>
        private CV.CorrelationVector InitializeCV(string value = null)
        {
            if (this.cv == null)
            {
                lock (this.lockObj)
                {
                    if (this.cv == null)
                    {
                        if (string.IsNullOrEmpty(value))
                        {
                            this.cv = new CV.CorrelationVector();
                        }
                        else
                        {
                            this.cv = CV.CorrelationVector.Extend(value);
                        }
                    }
                }
            }

            return this.cv;
        }
    }
}
