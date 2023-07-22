// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.TemplateBuilder.Fragments
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Practices.ObjectBuilder2;
    using Microsoft.PrivacyServices.Common.Context;

    /// <summary>
    ///     template fragment that is itself a template
    /// </summary>
    public class TemplateFragment : IFragment
    {
        private readonly IFragmentFactory fragFactory;

        private ICollection<string> variables;
        private IList<IFragment> inner;

        /// <summary>
        ///     Initializes a new instance of the ForeachFragment class
        /// </summary>
        /// <param name="fragmentFactory">frag factory</param>
        public TemplateFragment(IFragmentFactory fragmentFactory)
        {
            this.fragFactory = fragmentFactory ?? throw new ArgumentNullException(nameof(fragmentFactory));
        }

        /// <summary>
        ///     Gets the fragment type
        /// </summary>
        public FragmentType Type => FragmentType.Template;

        /// <summary>
        ///     Parses the section of the string to extract the type
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="source">source string to parse</param>
        /// <param name="idxStart">index start</param>
        /// <param name="length">maximum length to parse</param>
        /// <returns>index to start next parse at</returns>
        public int Parse(
            IContext context,
            string source, 
            int idxStart, 
            int length)
        {
            List<IFragment> newFrags = new List<IFragment>();
            int len = length;
            int idx = idxStart;

            if (this.inner != null)
            {
                throw new InvalidOperationException("object has already been initialized");
            }

            // the general operation is to 
            //  1. read a const string fragment until we hit an operation fragment
            //  2. read the operation fragment (and anything nested within)
            //  3. repeat at 1

            while (len > 0)
            {
                // whitespace is treated as significant within a const string, so we don't want to trim it out when in that stage

                IFragment frag;
                string tagOpFrag;
                int idxOpFrag = source.IndexOf(FragmentConsts.OpPrefix, idx, len, StringComparison.Ordinal);
                int lenOpFrag;
                int lenText = idxOpFrag < 0 ? len : idxOpFrag - idx;

                // if we have any constant text, add it to the 
                if (lenText > 0)
                {
                    frag = this.fragFactory.CreateConstFragment();
                    frag.Parse(context, source, idx, lenText);
                    newFrags.Add(frag);

                    idx += lenText;
                    len -= lenText;
                }

                // we have no further operations, which means we've burned through all the text as well, so we can bail
                if (idxOpFrag < 0)
                {
                    break;
                }

                // since we're going to overwrite len below when hunting for the tag, capture it here so we can it later
                lenOpFrag = len;

                // skip ahead to the next whitespace because all op fragments must have at least one whitespace character following
                //  its opening sequence
                // ReSharper disable once EmptyEmbeddedStatement
                for (; len > 0 && char.IsWhiteSpace(source[idx]) == false; ++idx, --len);

                if (len == 0)
                {
                    throw new TemplateParseException(
                        $"Template fragment starting at {idxStart} has invalid op fragment starting at {idxOpFrag}");
                }

                // advance idx by 1 to make sure we skip past the whitespace character we stopped on- it should be included in the
                //  tag
                tagOpFrag = source.Substring(idxOpFrag, (idx + 1) - idxOpFrag);

                try
                {
                    frag = this.fragFactory.CreateOpFragment(tagOpFrag);
                }
                catch (DependencyMissingException)
                {
                    throw new TemplateParseException(
                        $"Template fragment starting at {idxStart} has unknown op fragment {tagOpFrag} starting at {idxOpFrag}");
                }

                idx = frag.Parse(context, source, idxOpFrag, lenOpFrag);
                newFrags.Add(frag);

                len = length - (idx - idxStart);
            }

            this.inner = newFrags;

            return idx;
        }

        /// <summary>
        ///     Gets the set of variables referenced by the template
        /// </summary>
        /// <returns>resulting value</returns>
        public ICollection<string> GetVariables()
        {
            return this.variables ?? 
                   (this.variables = new ReadOnlyCollection<string>(this.inner.SelectMany(frag => frag.GetVariables()).ToList()));
        }

        /// <summary>
        ///     Applies the specified model to the template fragment
        /// </summary>
        /// <param name="context">render context</param>
        /// <param name="target">target StringBuilder to write to. Can be null to generate a new builder</param>
        /// <param name="model">data to be used for replacement variables</param>
        /// <returns>source or a created StringBuilder if target is null</returns>
        public StringBuilder Render(
            IContext context,
            StringBuilder target, 
            object model)
        {
            if (this.inner == null)
            {
                throw new InvalidOperationException("object has not yet been initialized");
            }

            target = target ?? new StringBuilder();

            foreach (IFragment frag in this.inner)
            {
                target = frag.Render(context, target, model);
            }

            return target;
        }

        /// <summary>
        ///     returns a string that represents this instance
        /// </summary>
        /// <returns>string that represents this instance</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("[TEMPLATE]<");

            foreach (IFragment frag in this.inner)
            {
                sb.Append(frag.ToString());
            }

            sb.Append(">");

            return sb.ToString();
        }
    }
}
