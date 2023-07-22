namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates;
    using Microsoft.PrivacyServices.Policy;

    /// <summary>
    /// Defines an interface representing a generic privacy command.
    /// </summary>
    public interface IDeleteCommand : IPrivacyCommand
    {
        /// <summary>
        /// The Privacy Data Type of this command.
        /// </summary>
        DataTypeId PrivacyDataType { get; }

        /// <summary>
        /// A predicate that is specific to the type of data being deleted. The type of this predicate depends on the Privacy Data Type of the command.
        /// Not all data types have a custom predicate. This predicate represents a logical AND operation with the <seealso cref="TimeRangePredicate"/>.
        /// </summary>
        IPrivacyPredicate DataTypePredicate { get; }

        /// <summary>
        /// A predicate that specifies a range of times to delete. This represents a logical AND 
        /// with the <seealso cref="DataTypePredicate"/>, when it is availalbe.
        /// </summary>
        TimeRangePredicate TimeRangePredicate { get; }
    }
}