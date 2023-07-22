namespace Microsoft.Azure.ComplianceServices.Common
{

    /// <summary>
    /// Compare two instances of an object for equality.
    /// </summary>
    public delegate bool Compare(object value1, object value2);

    public interface ICustomOperatorContext
    {
        /// <summary>
        /// Value for which filter needs to be evaluated.
        /// </summary>
        object Value { get; set; }

        /// <summary>
        /// Key, if specified in the filter definition, first key is matched and then filter is evaluated.
        /// </summary>
        string Key { get; set; }

        /// <summary>
        /// Delegate to compare two "Values" and check if those are same.
        /// </summary>
        Compare Compare { get; set; }

        /// <summary>
        /// Identifies current Service name optional.
        /// </summary>
        string ServiceName { get; set; }

        /// <summary>
        /// Current Environment name
        /// </summary>
        string EnvironmentName { get; set; }
        
        /// <summary>
        /// Name of the machine
        /// </summary>
        string MachineName { get; set; }

        /// <summary>
        /// Gets the build version.
        /// </summary>
        string AssemblyVersion { get; set; }

        /// <summary>
        /// Operation Name of the incoming event.
        /// </summary>
        string IncomingOperationName { get; set; }

        /// <summary>
        /// Caller name of the incoming event.
        /// </summary>
        string IncomingCallerName { get; set; }

        /// <summary>
        /// Local culture name.
        /// </summary>
        string Market { get; set; }
    }
}