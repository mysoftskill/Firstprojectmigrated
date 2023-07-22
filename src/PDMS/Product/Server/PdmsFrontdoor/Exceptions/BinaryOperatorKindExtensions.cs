namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    using System;

    using Microsoft.OData.UriParser;

    /// <summary>
    /// The operator in the query. This is normalized to contain all possible filter criteria values.
    /// </summary>
    public enum OperatorKind
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
        Equals,

        /// <summary>
        /// Corresponds to string.contains().
        /// </summary>
        Contains
    }

    /// <summary>
    /// Extension methods for the BinaryOperatorKind type.
    /// </summary>
    public static class BinaryOperatorKindExtensions
    {
        /// <summary>
        /// Converts the value into a comparison type.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The converted type.</returns>
        public static OperatorKind AsOperatorKind(this BinaryOperatorKind value)
        {
            switch (value)
            {
                case BinaryOperatorKind.Equal:
                    return OperatorKind.Equals;
                case BinaryOperatorKind.GreaterThan:
                    return OperatorKind.GreaterThan;
                case BinaryOperatorKind.GreaterThanOrEqual:
                    return OperatorKind.GreaterThanOrEquals;
                case BinaryOperatorKind.LessThan:
                    return OperatorKind.LessThan;
                case BinaryOperatorKind.LessThanOrEqual:
                    return OperatorKind.LessThanOrEquals;
                case BinaryOperatorKind.NotEqual:
                    return OperatorKind.NotEquals;
                default:
                    throw new Exception();
            }
        }
    }
}