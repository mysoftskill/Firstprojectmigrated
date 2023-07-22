namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
{
    /// <summary>
    /// Enum describing number (integer, long, double, etc.) comparison type.
    /// </summary>
    public enum NumberComparisonType
    {
        /// <summary>
        /// Corresponds to >.
        /// </summary>
        GreaterThan,

        /// <summary>
        /// Corresponds to >=.
        /// </summary>
        GreaterThanOrEquals,
        
        /// <summary>
        /// <![CDATA[Corresponds to <.]]> 
        /// </summary>
        LessThan,

        /// <summary>
        /// <![CDATA[Corresponds to <=.]]>         
        /// </summary>
        LessThanOrEquals,

        /// <summary>
        /// Corresponds to !=.
        /// </summary>
        NotEquals,

        /// <summary>
        /// Corresponds to =.
        /// </summary>
        Equals
    }
}