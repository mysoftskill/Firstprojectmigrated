namespace Microsoft.Azure.ComplianceServices.Common
{
    public class CustomOperatorFilterSettings
    {
        /// <summary>
        /// Operator, provides clues to how arguments are to be compared against provided context.
        /// Example:- Include will determine Feature to be enabled if the passed value in context in 
        /// EvaluteAsync in CustomOperatorFilter is present in Arguments whereas Exclude will determine 
        /// Feature to be disabled in the same circumstances.
        /// </summary>
        public string Operator { get; set; }

        /// <summary>
        /// Key, provides more context to filter evaluation, when set Key is matched before filter is evaluated.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Value, comma separated string.
        /// </summary>
        public string Value { get; set; }
        
        /// <summary>
        /// Current Service name.
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Current Environment name
        /// </summary>
        public string EnvironmentName { get; set; }

        /// <summary>
        /// Name of the machine
        /// </summary>
        public string MachineName { get; set; }

        /// <summary>
        /// Gets the build version.
        /// </summary>
        public string AssemblyVersion { get; set; }

        /// <summary>
        /// Location settings for the feature
        /// </summary>
        public string Market { get; set; }

        /// <summary>
        /// Operation Name of the incoming event.
        /// </summary>
        public string IncomingOperationName { get; set; }

        /// <summary>
        /// Caller Name of the incoming event.
        /// </summary>
        public string IncomingCallerName { get; set; }
    }
}