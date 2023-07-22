// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers
{
    using System;
    using System.Globalization;
    using Microsoft.Data.Edm.Library;
    using Microsoft.Data.OData;
    using Microsoft.Data.OData.Query;
    using Microsoft.Data.OData.Query.SemanticAst;

    /// <summary>
    /// Comparison type used in the filter expression
    /// </summary>
    public enum Comparison
    {
        Equal,
        GreaterThanEqual,
        LessThanEqual,
        Between,
    }

    /// <summary>
    /// This class parses the $filter date range expression and returns a simplified result object
    /// </summary>
    /// <remarks>
    /// Supported expressions are:
    ///     date eq datetime'YYYY-MM-DD'
    ///     datetime le datetime'YYYY-MM-DDTHH:MM:SS'
    ///     datetime ge datetime'YYYY-MM-DDTHH:MM:SS'
    ///     date ge datetime'YYYY-MM-DD' and date le datetime'YYYY-MM-DD'
    /// </remarks>
    public sealed class DateFilterResult
    {
        private const string SupportedDateRangeExpressionErrorText = "Only supported date range format is \"date ge datetime'YYYY-MM-DD' and date le datetime'YYYY-MM-DD'\"";
        private static readonly Tuple<EdmModel, EdmEntityType> modelEntityTuple = CreateModelEntityTuple();

        private DateFilterResult()
        {
        }

        /// <summary>
        /// Comparison type specified in expression
        /// </summary>
        public Comparison Comparison { get; private set; }

        /// <summary>
        /// First date of the expression. Meaning depends on Comparison type.
        /// Equal, GreaterThanEqual, LessThanEqual -> This is the single date used for comparison.
        /// Between: This is the start of the date range (inclusive)
        /// </summary>
        public DateTime? StartDate { get; private set; }

        /// <summary>
        /// Second date of the expression. Only used for comparison type 'Between'.
        /// The end of the date range (inclusive)
        /// </summary>
        public DateTime? EndDate { get; private set; }

        /// <summary>
        /// Parses the date range expression
        /// </summary>
        /// <param name="filter">Filter string</param>
        /// <returns>Date filter result</returns>
        public static DateFilterResult ParseFromFilter(string filter)
        {
            try
            {
                var filterClause = ODataUriParser.ParseFilter(filter, modelEntityTuple.Item1, modelEntityTuple.Item2);

                var outerBinaryNode = filterClause.Expression as BinaryOperatorNode;
                Comparison comparison = GetComparisonFromBinaryOperatorNode(outerBinaryNode);

                // For simple comparisons, return a single date and comparison type
                if (comparison != Comparison.Between)
                {
                    Tuple<Comparison, DateTime> leafComparison = GetLeafComparisonTuple(outerBinaryNode);
                    return new DateFilterResult
                    {
                        Comparison = leafComparison.Item1,
                        StartDate = leafComparison.Item2,
                    };
                }

                Tuple<Comparison, DateTime> leftComparison = GetLeafComparisonTuple(outerBinaryNode.Left as BinaryOperatorNode);
                Tuple<Comparison, DateTime> rightComparison = GetLeafComparisonTuple(outerBinaryNode.Right as BinaryOperatorNode);
                if (leftComparison == null || rightComparison == null)
                {
                    throw new NotSupportedException("Both sides of the 'and' must be binary operations. " + SupportedDateRangeExpressionErrorText);
                }

                if (leftComparison.Item1 != Comparison.GreaterThanEqual)
                {
                    throw new NotSupportedException("Left side of and expression must use 'ge' comparison type. " + SupportedDateRangeExpressionErrorText);
                }

                if (rightComparison.Item1 != Comparison.LessThanEqual)
                {
                    throw new NotSupportedException("Right side of and expression must use 'le' comparison type. " + SupportedDateRangeExpressionErrorText);
                }

                if (leftComparison.Item2 > rightComparison.Item2)
                {
                    throw new NotSupportedException("Invalid date range specified. No dates meet the given expression.");
                }

                return new DateFilterResult
                {
                    Comparison = comparison,
                    StartDate = leftComparison.Item2,
                    EndDate = rightComparison.Item2,
                };
            }
            catch (ODataException e)
            {
                throw new NotSupportedException("Error parsing $filter. See inner exception for details.", e);
            }
        }

        /// <summary>
        /// Creates a simple date model that used by the Odata parsing library.
        /// </summary>
        /// <returns>Tuple of data model and entity type</returns>
        private static Tuple<EdmModel, EdmEntityType> CreateModelEntityTuple()
        {
            var model = new EdmModel();
            var dateTimeEntityType = new EdmEntityType("Privacy", "Resource");
            var idProperty = dateTimeEntityType.AddStructuralProperty("Id", EdmCoreModel.Instance.GetInt32(false));
            dateTimeEntityType.AddKeys(idProperty);
            dateTimeEntityType.AddStructuralProperty("date", EdmCoreModel.Instance.GetDateTime(true));
            dateTimeEntityType.AddStructuralProperty("datetime", EdmCoreModel.Instance.GetDateTime(true));
            model.AddElement(dateTimeEntityType);

            return Tuple.Create(model, dateTimeEntityType);
        }

        private static Tuple<Comparison, DateTime> GetLeafComparisonTuple(BinaryOperatorNode binaryNode)
        {
            if (binaryNode == null)
            {
                throw new NotSupportedException("Expression is not of a supported complexity.");
            }

            Comparison comparison = GetComparisonFromBinaryOperatorNode(binaryNode);
            var rightConvertNode = binaryNode.Right as ConvertNode;
            if (rightConvertNode == null)
            {
                throw new NotSupportedException("Right side of expression is not a supported expression type.");
            }

            var rightConstant = rightConvertNode.Source as ConstantNode;
            if (rightConstant == null)
            {
                throw new NotSupportedException("Right side expression MUST contain a constant.");
            }

            var specifiedDate = ((DateTime)rightConstant.Value);
            DateTime date = DateTime.SpecifyKind(specifiedDate, DateTimeKind.Utc);

            return Tuple.Create(comparison, date);
        }

        private static Comparison GetComparisonFromBinaryOperatorNode(BinaryOperatorNode binaryNode)
        {
            Comparison comparison;
            switch (binaryNode.OperatorKind)
            {
                case BinaryOperatorKind.Equal:
                    comparison = Comparison.Equal;
                    break;
                case BinaryOperatorKind.GreaterThanOrEqual:
                    comparison = Comparison.GreaterThanEqual;
                    break;
                case BinaryOperatorKind.LessThanOrEqual:
                    comparison = Comparison.LessThanEqual;
                    break;
                case BinaryOperatorKind.And:
                    comparison = Comparison.Between;
                    break;
                default:
                    var errorMessage = string.Format(CultureInfo.InvariantCulture, "Unsupported comparison type '{0}' in filter expression.", binaryNode.OperatorKind);
                    throw new NotSupportedException(errorMessage);
            }

            return comparison;
        }
    }
}
