// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.TemplateBuilder.Fragments
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;

    /// <summary>
    ///     template fragment that is a replacement variable
    /// </summary>
    public class VarFragment : OpFragment
    {
        private const string SequenceActual = "var";
        private const string PrefixActual = FragmentConsts.OpPrefix + VarFragment.SequenceActual + " ";
        private const string SuffixActual = " " + VarFragment.SequenceActual + FragmentConsts.OpSuffix;

        private static readonly IEnumerable<string> MissingProps = new List<string> { "variable" };

        private readonly IModelManipulator modelManipulator;

        private ICollection<string> variables;
        private string defValue;
        private string format;
        private string selector;

        /// <summary>
        ///     Initializes a new instance of the VarFragment class
        /// </summary>
        /// <param name="modelManipulator">model manipulator</param>
        public VarFragment(IModelManipulator modelManipulator)
        {
            this.modelManipulator = modelManipulator ?? throw new ArgumentNullException(nameof(modelManipulator));
        }

        /// <summary>
        ///     Gets prefix sequence
        /// </summary>
        public static string PrefixSequence => VarFragment.PrefixActual;

        /// <summary>
        ///     Gets the fragment type
        /// </summary>
        public override FragmentType Type => FragmentType.Variable;

        /// <summary>
        ///     Gets the prefix
        /// </summary>
        protected override string Prefix => VarFragment.PrefixActual;

        /// <summary>
        ///     Gets the suffix
        /// </summary>
        protected override string Suffix => VarFragment.SuffixActual;

        /// <summary>
        ///     Parses the section of the string to extract the type
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="source">source string to parse</param>
        /// <param name="idxStart">index start</param>
        /// <param name="length">maximum length to parse</param>
        /// <returns>index to start next parse at</returns>
        public override int Parse(
            IContext context,
            string source, 
            int idxStart, 
            int length)
        {
            if (this.selector != null)
            {
                throw new InvalidOperationException("object has already been initialized");
            }
                
            return this.ParseTag("VarFragment at position " + idxStart.ToStringInvariant(), source, idxStart, length).IdxNext;
        }

        /// <summary>
        ///     Gets the set of variables referenced by the template
        /// </summary>
        /// <returns>resulting value</returns>
        public override ICollection<string> GetVariables()
        {
            return this.variables ?? (this.variables = new ReadOnlyCollection<string>(new List<string> { this.selector }));
        }

        /// <summary>
        ///     Applies the specified source
        /// </summary>
        /// <param name="context">render context</param>
        /// <param name="target">target StringBuilder to write to. Can be null to generate a new builder</param>
        /// <param name="model">data to be used for replacement variables</param>
        /// <returns>source or a created StringBuilder if target is null</returns>
        public override StringBuilder Render(
            IContext context,
            StringBuilder target,
            object model)
        {
            if (this.selector == null)
            {
                throw new InvalidOperationException("object has not yet been initialized");
            }

            target = target ?? new StringBuilder();
            target.Append(this.GetValueAsString(context, model));
            return target;
        }

        /// <summary>
        ///     returns a string that represents this instance
        /// </summary>
        /// <returns>string that represents this instance</returns>
        public override string ToString()
        {
            string formatText = this.format != null ? ("'" + this.format + "'") : "null";
            string defValText = this.defValue != null ? ("'" + this.defValue + "'") : "null";

            return $"[VAR sel:'{this.selector}' format:{formatText} default:{defValText}]";
        }

        /// <summary>
        ///     sets a found property on the derived object
        /// </summary>
        /// <param name="name">property name</param>
        /// <param name="value">property value</param>
        /// <returns>false if the property is not valid or supported</returns>
        protected override PropResponse SetPropery(
            string name,
            string value)
        {
            Action<string> setter;
            string current;

            value = value.Trim();
            if (string.IsNullOrEmpty(value))
            {
                return PropResponse.BadFormat;
            }

            if ((name.Length == 1 && name[0] == 's') ||
                "sel".Equals(name, StringComparison.Ordinal) ||
                "selector".Equals(name, StringComparison.Ordinal))
            {
                current = this.selector;
                setter = v => this.selector = v;
            }
            else if ((name.Length == 1 && name[0] == 'd') || 
                     "def".Equals(name, StringComparison.Ordinal) ||
                     "default".Equals(name, StringComparison.Ordinal))
            {
                current = this.defValue;
                setter = v => this.defValue = v;
            }
            else if ((name.Length == 1 && name[0] == 'f') || 
                     "fmt".Equals(name, StringComparison.Ordinal) ||
                     "format".Equals(name, StringComparison.Ordinal))
            {
                current = this.format;
                setter = v => this.format = v;
            }
            else
            {
                return PropResponse.NotSupported;
            }

            if (current != null)
            {
                return PropResponse.Duplicate;
            }

            setter(value);
            return PropResponse.Ok;
        }

        /// <summary>
        ///     Verifies all requried properties are present
        /// </summary>
        /// <returns>collection of missing properties (or an empty enumerable if none)</returns>
        protected override IEnumerable<string> VerifyRequiredProperties()
        {
            return this.selector != null ? Enumerable.Empty<string>() : VarFragment.MissingProps;
        }

        /// <summary>
        ///     Gets the value as string
        /// </summary>
        /// <param name="context">context</param>
        /// <param name="source">source model to select value out of</param>
        /// <returns>resulting string value</returns>
        private string GetValueAsString(
            IContext context,
            object source)
        {
            object valueRaw;

            if (this.modelManipulator.TryExtractValue(context, source, this.selector, out valueRaw) == false)
            {
                return this.defValue ?? string.Empty;
            }

            if (valueRaw == null)
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(this.format) == false && valueRaw is IFormattable valueFormattable)
            {
                return valueFormattable.ToString(this.format, CultureInfo.InvariantCulture);
            }

            return valueRaw.ToString();
        }
    }
}
