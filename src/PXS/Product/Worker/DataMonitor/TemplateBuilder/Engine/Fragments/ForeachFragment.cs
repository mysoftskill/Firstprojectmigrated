// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.TemplateBuilder.Fragments
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;

    /// <summary>
    ///     template fragment that is a replacement variable
    /// </summary>
    public class ForeachFragment : OpFragment
    {
        private const string SequenceActual = "foreach";
        private const string PrefixNoSpace = FragmentConsts.OpPrefix + ForeachFragment.SequenceActual;
        private const string PrefixActual = ForeachFragment.PrefixNoSpace + " ";
        private const string SuffixActual = " " + ForeachFragment.SequenceActual + FragmentConsts.OpSuffix;
        private const string EndLoop = "[[<foreachend>]]";

        private static readonly IEnumerable<string> MissingProps = new List<string> { "variable" };

        private readonly IModelManipulator modelManipulator;
        private readonly IFragmentFactory fragFactory;

        private ICollection<string> variables;
        private IFragment inner;
        private string separator;
        private string selector;

        /// <summary>
        ///     Initializes a new instance of the ForeachFragment class
        /// </summary>
        /// <param name="modelManipulator">model manipulator</param>
        /// <param name="fragmentFactory">frag factory</param>
        public ForeachFragment(
            IModelManipulator modelManipulator,
            IFragmentFactory fragmentFactory)
        {
            this.modelManipulator = modelManipulator ?? throw new ArgumentNullException(nameof(modelManipulator));
            this.fragFactory = fragmentFactory ?? throw new ArgumentNullException(nameof(fragmentFactory));
        }

        /// <summary>
        ///     Gets prefix sequence
        /// </summary>
        public static string PrefixSequence => ForeachFragment.PrefixActual;

        /// <summary>
        ///     Gets the fragment type
        /// </summary>
        public override FragmentType Type => FragmentType.Foreach;

        /// <summary>
        ///     Gets the prefix
        /// </summary>
        protected override string Prefix => ForeachFragment.PrefixActual;

        /// <summary>
        ///     Gets the suffix
        /// </summary>
        protected override string Suffix => ForeachFragment.SuffixActual;

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
            IFragment innerTemplate;
            string errPrefix;
            int idxLoopStart;
            int idxLoopEnd = 0;
            int nested = 0;
            int idx;
            int len;

            if (this.inner != null)
            {
                throw new InvalidOperationException("object has already been initialized");
            }

            errPrefix = "ForeachFragment at position " + idxStart.ToStringInvariant();

            (idxLoopStart, len) = this.ParseTag(errPrefix, source, idxStart, length);

            // find where the loop ends.  However, since a foreach loop can contain nested foreach loops, we have to take care
            //  to ensure that we skip over any end-of-loop markers that are the end of a nested loop.
            for (idx = idxLoopStart; len > 0; )
            {
                int idxCandidate = source.IndexOf(ForeachFragment.PrefixNoSpace, idx, len, StringComparison.Ordinal);
                if (idxCandidate < 0)
                {
                    throw new TemplateParseException(errPrefix + " could not find end of loop marker " + ForeachFragment.EndLoop);
                }

                if (string.CompareOrdinal(source, idxCandidate, ForeachFragment.EndLoop, 0, ForeachFragment.EndLoop.Length) == 0)
                {
                    if (nested == 0)
                    {
                        idxLoopEnd = idxCandidate;
                        break;
                    }

                    --nested;
                }
                else
                {
                    int compare = string.CompareOrdinal(
                        source,
                        idxCandidate,
                        ForeachFragment.PrefixActual,
                        0,
                        ForeachFragment.PrefixActual.Length);

                    if (compare == 0)
                    {
                        ++nested;
                    }
                }

                // skip over the marker to make sure we don't pick it up again
                idxCandidate += ForeachFragment.PrefixNoSpace.Length;

                // adjust the length and index pointer for the next pass
                len -= idxCandidate - idx;
                idx = idxCandidate;
            }

            // parse out the inner template
            innerTemplate = this.fragFactory.CreateTemplateFragment();

            // idxLoopStart is the start of the template and idxLoopEnd is the character after the last one we want to allow for
            //  the inner template, so the allowed length is idxLoopEnd - idxLoopStart
            innerTemplate.Parse(context, source, idxLoopStart, idxLoopEnd - idxLoopStart);

            this.inner = innerTemplate;

            if (idxLoopEnd < 0)
            {
                throw new TemplateParseException(
                    errPrefix + " could not find end of loop marker " + ForeachFragment.EndLoop);
            }

            return idxLoopEnd + ForeachFragment.EndLoop.Length;
        }

        /// <summary>
        ///     Applies the specified model to the template fragment
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
            IEnumerable enumer;
            string sep = string.IsNullOrEmpty(this.separator) ? null : this.separator;
            bool addSeparator = false;

            if (this.inner == null)
            {
                throw new InvalidOperationException("object has not yet been initialized");
            }

            enumer = this.modelManipulator.TryExtractValue(context, model, this.selector, out object objRaw) ? 
                this.modelManipulator.ToEnumerable(objRaw) : 
                Enumerable.Empty<object>();

            target = target ?? new StringBuilder();

            foreach (object subModel in enumer)
            {
                if (sep != null)
                {
                    if (addSeparator)
                    {
                        target.Append(sep);
                    }
                    else
                    {
                        addSeparator = true;
                    }
                }

                target = this.inner.Render(context, target, subModel);
            }

            return target;
        }

        /// <summary>
        ///     returns a string that represents this instance
        /// </summary>
        /// <returns>string that represents this instance</returns>
        public override string ToString()
        {
            string sepText = string.IsNullOrEmpty(this.separator) ? "null" : "'" + this.separator + "'";
            return $"[FOREACH sel:'{this.selector}' sep:{sepText}]<" + this.inner.ToString() + ">";
        }

        /// <summary>
        ///     Gets the set of variables referenced by the template
        /// </summary>
        /// <returns>resulting value</returns>
        public override ICollection<string> GetVariables()
        {
            if (this.variables == null)
            {
                List<string> vars = new List<string> { this.selector };
                vars.AddRange(this.inner.GetVariables().Select(o => this.selector + "[]." + o));
                this.variables = new ReadOnlyCollection<string>(vars);
            }

            return this.variables;
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
            
            if ("sel".Equals(name, StringComparison.Ordinal) ||
                "selector".Equals(name, StringComparison.Ordinal))
            {
                value = value.Trim();
                if (string.IsNullOrEmpty(value))
                {
                    return PropResponse.BadFormat;
                }

                current = this.selector;
                setter = v => this.selector = v;
            }
            else if ("sep".Equals(name, StringComparison.Ordinal) ||
                     "separator".Equals(name, StringComparison.Ordinal))
            {
                current = this.separator;
                setter = v => this.separator = v;
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
            return this.selector != null ? Enumerable.Empty<string>() : ForeachFragment.MissingProps;
        }
    }
}
