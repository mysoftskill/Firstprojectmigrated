namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Controllers
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    using Microsoft.AspNet.OData.Query;
    using Microsoft.OData.UriParser;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;

    /// <summary>
    /// A module that contains extension methods for filter criteria.
    /// </summary>
    public static class FilterCriteriaModule
    {
        /// <summary>
        /// Creates a filter criteria from the given OData query options.
        /// </summary>
        /// <typeparam name="TApi">The API entity type.</typeparam>
        /// <typeparam name="TCore">The Core entity type.</typeparam>
        /// <typeparam name="TFilterCriteria">The Core entity's filter criteria type.</typeparam>
        /// <param name="options">The OData query options.</param>
        /// <param name="setProperty">An action to help initialize values.</param>
        /// <returns>A filter criteria object.</returns>
        public static IFilterCriteria<TCore> Create<TApi, TCore, TFilterCriteria>(
            ODataQueryOptions<TApi> options,
            Action<TFilterCriteria, string, object, OperatorKind> setProperty) where TFilterCriteria : IFilterCriteria<TCore>
        {
            IFilterCriteria<TCore> filterCriteria = null;

            if (options.Filter?.FilterClause != null)
            {
                var node = options.Filter.FilterClause.Expression;

                filterCriteria = BuildFilter<TCore, TFilterCriteria>(node, setProperty);
            }
            else
            {
                filterCriteria = Activator.CreateInstance<TFilterCriteria>();
            }

            filterCriteria.Initialize(options);

            return filterCriteria;
        }

        /// <summary>
        /// Initializes the filter criteria object with paging information
        /// based on the given OData query options.
        /// </summary>
        /// <typeparam name="TApi">The API entity type.</typeparam>
        /// <param name="filterCriteria">The filter criteria to initialize.</param>
        /// <param name="options">The OData query options.</param>
        public static void Initialize<TApi>(this IFilterCriteria filterCriteria, ODataQueryOptions<TApi> options)
        {
            filterCriteria.Count = options?.Top?.Value;
            filterCriteria.Index = options?.Skip?.Value;
        }

        /// <summary>
        /// Builds a new filter criteria based on the given node information.
        /// </summary>
        /// <typeparam name="TCore">The Core entity type.</typeparam>
        /// <typeparam name="TFilterCriteria">The Core entity's filter criteria type.</typeparam>
        /// <param name="node">A node in the filter expression.</param>
        /// <param name="setProperty">An action to help initialize values.</param>
        /// <returns>The newly created filter criteria.</returns>
        public static IFilterCriteria<TCore> BuildFilter<TCore, TFilterCriteria>(
            SingleValueNode node,
            Action<TFilterCriteria, string, object, OperatorKind> setProperty) where TFilterCriteria : IFilterCriteria<TCore>
        {
            if (node.Kind == QueryNodeKind.BinaryOperator)
            {
                var binaryNode = node as BinaryOperatorNode;

                // Must handle OR separately because it needs to combine two fully built filter criteria.
                // The initialize method below can only construct a single filter criteria.
                // We call Build recursively so that if there are multiple OR statements, they get nested.
                if (binaryNode.OperatorKind == BinaryOperatorKind.Or)
                {
                    var left = BuildFilter<TCore, TFilterCriteria>(binaryNode.Left, setProperty);
                    var right = BuildFilter<TCore, TFilterCriteria>(binaryNode.Right, setProperty);
                    return left.Or(right);
                }
            }

            var filter = Activator.CreateInstance<TFilterCriteria>();
            InitializeFilter(node, filter, setProperty);
            return filter;
        }

        /// <summary>
        /// Recursively parses the filter expression and calls the provided action to set the filter criteria.
        /// </summary>
        /// <typeparam name="TFilterCriteria">The Core entity's filter criteria type.</typeparam>
        /// <param name="node">A node in the filter expression.</param>
        /// <param name="filter">The filter criteria whose values should be set.</param>
        /// <param name="setProperty">An action to help initialize values.</param>
        [ExcludeFromCodeCoverage] // TODO: Enable once tested.
        private static void InitializeFilter<TFilterCriteria>(
            SingleValueNode node,
            TFilterCriteria filter,
            Action<TFilterCriteria, string, object, OperatorKind> setProperty) where TFilterCriteria : IFilterCriteria
        {
            if (node.Kind == QueryNodeKind.BinaryOperator)
            {
                var binaryNode = node as BinaryOperatorNode;

                switch (binaryNode.OperatorKind)
                {
                    case BinaryOperatorKind.And:
                        InitializeFilter(binaryNode.Left, filter, setProperty);
                        InitializeFilter(binaryNode.Right, filter, setProperty);
                        break;
                    case BinaryOperatorKind.Equal:
                    case BinaryOperatorKind.GreaterThan:
                    case BinaryOperatorKind.GreaterThanOrEqual:
                    case BinaryOperatorKind.LessThan:
                    case BinaryOperatorKind.LessThanOrEqual:
                    case BinaryOperatorKind.NotEqual:
                        var left = binaryNode.Left as SingleValuePropertyAccessNode;

                        if (left == null)
                        {
                            left = (binaryNode.Left as ConvertNode).Source as SingleValuePropertyAccessNode;
                        }

                        var right = binaryNode.Right as ConstantNode;

                        setProperty(filter, left.Property.Name, right?.Value, binaryNode.OperatorKind.AsOperatorKind());

                        break;
                }
            }
            else if (node.Kind == QueryNodeKind.Convert)
            {
                var convertNode = node as ConvertNode;
                InitializeFilter(convertNode.Source, filter, setProperty);
            }
            else if (node.Kind == QueryNodeKind.SingleValueFunctionCall)
            {
                var funcNode = node as SingleValueFunctionCallNode;

                if (funcNode.Name == "contains")
                {
                    var parameters = GetParameters(funcNode);

                    if (parameters != null)
                    {
                        setProperty(filter, parameters.Item1, parameters.Item2, OperatorKind.Contains);
                    }
                }
            }
        }

        /// <summary>
        /// Given a function node, extracts out the parameter name and value.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>A tuple where the first item is the name and the second is the value.</returns>
        [ExcludeFromCodeCoverage] // TODO: Enable once tested.
        private static Tuple<string, object> GetParameters(SingleValueFunctionCallNode node)
        {
            string propertyName = null;
            object propertyValue = null;

            foreach (var param in node.Parameters)
            {
                var propertyNameNode = param as SingleValuePropertyAccessNode;

                if (propertyNameNode != null)
                {
                    propertyName = propertyNameNode.Property.Name;
                }
                else
                {
                    var propertyValueNode = param as ConstantNode;

                    if (propertyValueNode != null)
                    {
                        propertyValue = propertyValueNode.Value;
                    }
                }
            }

            if (propertyName == null || propertyValue == null)
            {
                return null;
            }
            else
            {
                return new Tuple<string, object>(propertyName, propertyValue);
            }
        }
    }
}