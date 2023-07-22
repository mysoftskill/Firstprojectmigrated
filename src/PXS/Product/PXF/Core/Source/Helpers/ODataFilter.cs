// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Spatial;

    using Microsoft.Data.Edm;
    using Microsoft.Data.Edm.Library;
    using Microsoft.Data.Edm.Library.Expressions;
    using Microsoft.Data.OData;
    using Microsoft.Data.OData.Query;
    using Microsoft.Data.OData.Query.SemanticAst;

    /// <summary>
    ///     This class is responsible for handling OData $filter string processing
    /// </summary>
    public sealed class ODataFilter
    {
        private const double EarthRadius = 6371000;

        private readonly FilterClause filterClause;

        private readonly Func<object, string, object> getFilterPropertyFunc;

        /// <summary>
        ///     Constructs a new ODataFilter object that can be used to match items.
        /// </summary>
        /// <param name="properties">A map of property names to property types</param>
        /// <param name="filter">The OData filter string</param>
        /// <param name="getFilterPropertyFunc">A function to retrieve a named property from an object</param>
        public ODataFilter(
            IDictionary<string, EdmPrimitiveTypeKind> properties,
            string filter,
            Func<object, string, object> getFilterPropertyFunc)
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));
            if (getFilterPropertyFunc == null)
                throw new ArgumentNullException(nameof(getFilterPropertyFunc));

            var edmModel = new EdmModel();
            var edmType = new EdmEntityType("Privacy", "Resource");

            foreach (KeyValuePair<string, EdmPrimitiveTypeKind> pair in properties)
                edmType.AddStructuralProperty(pair.Key, pair.Value);
            edmModel.AddElement(edmType);

            // TODO: This is not yet working.
            //var func = new EdmFunction("Privacy", "containsAny", EdmCoreModel.Instance.GetBoolean(false), null);
            //func.AddParameter(new EdmFunctionParameter(func, "0", EdmCoreModel.Instance.GetString(true), EdmFunctionParameterMode.In));
            ////func.AddParameter(new EdmFunctionParameter(func, "1", EdmCoreModel.Instance.GetString(true), EdmFunctionParameterMode.In));
            ////func.AddParameter("a0", EdmCoreModel.Instance.GetString(true));
            //edmModel.AddElement(func);

            //var container = new EdmEntityContainer("Privacy", "Container");
            //container.AddEntitySet("Resources", edmType);
            //var import = new EdmFunctionImport(container, "containsAny", EdmCoreModel.Instance.GetBoolean(false), null, false, true, false);
            //import.AddParameter(new EdmFunctionParameter(import, "0", EdmCoreModel.Instance.GetString(true), EdmFunctionParameterMode.In));
            ////import.AddParameter(new EdmFunctionParameter(import, "1", EdmCoreModel.Instance.GetString(true), EdmFunctionParameterMode.In));
            //container.AddElement(import);
            //edmModel.AddElement(container);

            this.getFilterPropertyFunc = getFilterPropertyFunc;

            try
            {
                //var parser = new ODataUriParser(edmModel, null);
                this.filterClause = ODataUriParser.ParseFilter(filter, edmModel, edmType);
            }
            catch (ODataException e)
            {
                throw new NotSupportedException("Error parsing $filter. See inner exception for details.", e);
            }
        }

        /// <summary>
        ///     Tests if an object matches the OData $filter or not.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>True of the object passes the filter, false otherwise</returns>
        public bool Matches(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            return this.ToBoolean(this.Reduce(obj, this.filterClause.Expression));
        }

        private bool Compare(BinaryOperatorKind kind, object left, object right)
        {
            if (left is DateTimeOffset && right is DateTimeOffset)
            {
                var leftValue = (DateTimeOffset)left;
                var rightValue = (DateTimeOffset)right;

                switch (kind)
                {
                    // This eq/neq code is techincally wrong I believe. We compare only the date
                    // component when doing eq on two dates. This is behavior from legacy implementation.
                    case BinaryOperatorKind.Equal:
                        return leftValue.Date == rightValue.Date;
                    case BinaryOperatorKind.NotEqual:
                        return leftValue.Date != rightValue.Date;

                    case BinaryOperatorKind.GreaterThan:
                        return leftValue > rightValue;
                    case BinaryOperatorKind.GreaterThanOrEqual:
                        return leftValue >= rightValue;
                    case BinaryOperatorKind.LessThan:
                        return leftValue < rightValue;
                    case BinaryOperatorKind.LessThanOrEqual:
                        return leftValue <= rightValue;
                }
            }

            if (left is DateTime && right is DateTime)
            {
                var leftValue = (DateTime)left;
                var rightValue = (DateTime)right;

                switch (kind)
                {
                    // This eq/neq code is techincally wrong I believe. We compare only the date
                    // component when doing eq on two dates. This is behavior from legacy implementation.
                    case BinaryOperatorKind.Equal:
                        return leftValue.Date == rightValue.Date;
                    case BinaryOperatorKind.NotEqual:
                        return leftValue.Date != rightValue.Date;

                    case BinaryOperatorKind.GreaterThan:
                        return leftValue > rightValue;
                    case BinaryOperatorKind.GreaterThanOrEqual:
                        return leftValue >= rightValue;
                    case BinaryOperatorKind.LessThan:
                        return leftValue < rightValue;
                    case BinaryOperatorKind.LessThanOrEqual:
                        return leftValue <= rightValue;
                }
            }

            string leftStr = left as string;
            string rightStr = right as string;
            if ((leftStr != null) && (rightStr != null))
                switch (kind)
                {
                    case BinaryOperatorKind.Equal:
                        return leftStr == rightStr;
                    case BinaryOperatorKind.NotEqual:
                        return leftStr != rightStr;
                }

            if (left is int && right is int)
            {
                int leftValue = (int)left;
                int rightValue = (int)right;

                switch (kind)
                {
                    case BinaryOperatorKind.Equal:
                        return leftValue == rightValue;
                    case BinaryOperatorKind.NotEqual:
                        return leftValue != rightValue;

                    case BinaryOperatorKind.GreaterThan:
                        return leftValue > rightValue;
                    case BinaryOperatorKind.GreaterThanOrEqual:
                        return leftValue >= rightValue;
                    case BinaryOperatorKind.LessThan:
                        return leftValue < rightValue;
                    case BinaryOperatorKind.LessThanOrEqual:
                        return leftValue <= rightValue;
                }
            }

            if (left is long && right is long)
            {
                long leftValue = (long)left;
                long rightValue = (long)right;

                switch (kind)
                {
                    case BinaryOperatorKind.Equal:
                        return leftValue == rightValue;
                    case BinaryOperatorKind.NotEqual:
                        return leftValue != rightValue;

                    case BinaryOperatorKind.GreaterThan:
                        return leftValue > rightValue;
                    case BinaryOperatorKind.GreaterThanOrEqual:
                        return leftValue >= rightValue;
                    case BinaryOperatorKind.LessThan:
                        return leftValue < rightValue;
                    case BinaryOperatorKind.LessThanOrEqual:
                        return leftValue <= rightValue;
                }
            }

            if (left is double && right is double)
            {
                double leftValue = (double)left;
                double rightValue = (double)right;

                switch (kind)
                {
                    case BinaryOperatorKind.Equal:
                        return leftValue == rightValue;
                    case BinaryOperatorKind.NotEqual:
                        return leftValue != rightValue;

                    case BinaryOperatorKind.GreaterThan:
                        return leftValue > rightValue;
                    case BinaryOperatorKind.GreaterThanOrEqual:
                        return leftValue >= rightValue;
                    case BinaryOperatorKind.LessThan:
                        return leftValue < rightValue;
                    case BinaryOperatorKind.LessThanOrEqual:
                        return leftValue <= rightValue;
                }
            }

            if (left is float && right is float)
            {
                float leftValue = (float)left;
                float rightValue = (float)right;

                switch (kind)
                {
                    case BinaryOperatorKind.Equal:
                        return leftValue == rightValue;
                    case BinaryOperatorKind.NotEqual:
                        return leftValue != rightValue;

                    case BinaryOperatorKind.GreaterThan:
                        return leftValue > rightValue;
                    case BinaryOperatorKind.GreaterThanOrEqual:
                        return leftValue >= rightValue;
                    case BinaryOperatorKind.LessThan:
                        return leftValue < rightValue;
                    case BinaryOperatorKind.LessThanOrEqual:
                        return leftValue <= rightValue;
                }
            }

            if (left is Guid && right is Guid)
            {
                var leftValue = (Guid)left;
                var rightValue = (Guid)right;

                switch (kind)
                {
                    case BinaryOperatorKind.Equal:
                        return leftValue == rightValue;
                    case BinaryOperatorKind.NotEqual:
                        return leftValue != rightValue;
                }
            }

            throw new NotSupportedException($"Unknown comparison {kind} with data types {left.GetType().FullName} and {right.GetType().FullName}");
        }

        private object ExecuteFunc(object obj, SingleValueFunctionCallNode functionNode)
        {
            SingleValueNode[] args = functionNode.Arguments.Cast<SingleValueNode>().ToArray();

            // There are a bunch more functions to implement: https://msdn.microsoft.com/en-us/library/hh169248(v=nav.90).aspx
            List<string> matches;
            List<string> collection;
            switch (functionNode.Name)
            {
                case "substringof":
                    return this.ToString(this.Reduce(obj, args[0])).Contains(this.ToString(this.Reduce(obj, args[1])));
                case "startswith":
                    return this.ToString(this.Reduce(obj, args[0])).StartsWith(this.ToString(this.Reduce(obj, args[1])));
                case "endswith":
                    return this.ToString(this.Reduce(obj, args[0])).EndsWith(this.ToString(this.Reduce(obj, args[1])));
                case "length":
                    return this.ToString(this.Reduce(obj, args[0])).Length;
                case "indexof":
                    return this.ToString(this.Reduce(obj, args[0]))
                        .IndexOf(this.ToString(this.Reduce(obj, args[1])), StringComparison.InvariantCulture);
                case "tolower":
                    return this.ToString(this.Reduce(obj, args[0])).ToLower();
                case "toupper":
                    return this.ToString(this.Reduce(obj, args[0])).ToUpper();
                case "trim":
                    return this.ToString(this.Reduce(obj, args[0])).Trim();
                case "concat":
                    return this.ToString(this.Reduce(obj, args[0])) + this.ToString(this.Reduce(obj, args[1]));
                case "geo.distance":
                    GeographyPoint left = this.ToGeographyPoint(this.Reduce(obj, args[0]));
                    GeographyPoint right = this.ToGeographyPoint(this.Reduce(obj, args[1]));
                    return CalculateDistance(left, right);
                case "containsAny":
                    matches = args.Skip(1).Select(a => this.ToString(this.Reduce(obj, a))).ToList();
                    collection = this.ToStringEnumerable(this.Reduce(obj, args[0])).ToList();
                    return matches.Any(e => collection.Contains(e));
                case "containsAll":
                    matches = args.Skip(1).Select(a => this.ToString(this.Reduce(obj, a))).ToList();
                    collection = this.ToStringEnumerable(this.Reduce(obj, args[0])).ToList();
                    return matches.All(e => collection.Contains(e));
            }

            throw new NotSupportedException($"Unknown function {functionNode.Name}");
        }

        private object Convert(dynamic obj, EdmPrimitiveTypeKind kind)
        {
            switch (kind)
            {
                case EdmPrimitiveTypeKind.Boolean:
                    return (bool)obj;
                case EdmPrimitiveTypeKind.Int32:
                    return (int)obj;
                case EdmPrimitiveTypeKind.Single:
                    return (float)obj;
                case EdmPrimitiveTypeKind.Double:
                    return (double)obj;
                case EdmPrimitiveTypeKind.DateTimeOffset:
                    return (DateTimeOffset)obj;
                case EdmPrimitiveTypeKind.GeographyPoint:
                    return (GeographyPoint)obj;
            }
            
            //throw new NotSupportedException();
            return (object)obj; // hope for the best
        }

        private object Reduce(object obj, SingleValueNode node)
        {
            switch (node.Kind)
            {
                case QueryNodeKind.Convert:
                    var convertNode = (ConvertNode)node;
                    return this.Convert(this.Reduce(obj, convertNode.Source), convertNode.TypeReference.PrimitiveKind());
                case QueryNodeKind.Constant:
                    var constantNode = (ConstantNode)node;
                    return constantNode.Value;
                case QueryNodeKind.SingleValuePropertyAccess:
                    var accessNode = (SingleValuePropertyAccessNode)node;
                    return this.getFilterPropertyFunc(obj, accessNode.Property.Name);
                case QueryNodeKind.BinaryOperator:
                    var binaryOperator = (BinaryOperatorNode)node;
                    switch (binaryOperator.OperatorKind)
                    {
                        case BinaryOperatorKind.And:
                            return this.ToBoolean(this.Reduce(obj, binaryOperator.Left)) && this.ToBoolean(this.Reduce(obj, binaryOperator.Right));
                        case BinaryOperatorKind.Or:
                            return this.ToBoolean(this.Reduce(obj, binaryOperator.Left)) || this.ToBoolean(this.Reduce(obj, binaryOperator.Right));
                        case BinaryOperatorKind.Equal:
                        case BinaryOperatorKind.NotEqual:
                        case BinaryOperatorKind.GreaterThan:
                        case BinaryOperatorKind.GreaterThanOrEqual:
                        case BinaryOperatorKind.LessThan:
                        case BinaryOperatorKind.LessThanOrEqual:
                            return this.Compare(binaryOperator.OperatorKind, this.Reduce(obj, binaryOperator.Left), this.Reduce(obj, binaryOperator.Right));
                    }
                    throw new NotSupportedException($"Cannot reduce BinaryOperator node with OperatorKind {binaryOperator.Kind}");
                case QueryNodeKind.SingleValueFunctionCall:
                    return this.ExecuteFunc(obj, (SingleValueFunctionCallNode)node);
            }

            throw new NotSupportedException($"Cannot reduce node of kind {node.Kind}");
        }

        private bool ToBoolean(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (!(obj is bool))
                throw new NotSupportedException($"Cannot convert {obj.GetType().FullName} to boolean");
            return (bool)obj;
        }

        private GeographyPoint ToGeographyPoint(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (!(obj is GeographyPoint))
                throw new NotSupportedException($"Cannot convert {obj.GetType().FullName} to GeographyPosition");
            return (GeographyPoint)obj;
        }

        private string ToString(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (!(obj is string))
                throw new NotSupportedException($"Cannot convert {obj.GetType().FullName} to string");
            return (string)obj;
        }

        private IEnumerable<string> ToStringEnumerable(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            return obj as IEnumerable<string>;
        }

        private static double CalculateDistance(GeographyPoint left, GeographyPoint right)
        {
            double dLat = ToRadians(right.Latitude - left.Latitude);
            double dLon = ToRadians(right.Longitude - left.Longitude);

            double a = Math.Pow(Math.Sin(dLat / 2), 2) +
                       Math.Cos(ToRadians(left.Latitude)) * Math.Cos(ToRadians(right.Latitude)) *
                       Math.Pow(Math.Sin(dLon / 2), 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            double distance = EarthRadius * c;
            return distance;
        }

        private static double ToRadians(double input)
        {
            return input * (Math.PI / 180);
        }
    }
}
