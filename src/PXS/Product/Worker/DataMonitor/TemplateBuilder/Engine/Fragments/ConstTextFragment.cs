// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.TemplateBuilder.Fragments
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.Common.Context;

    /// <summary>
    ///     template fragment that is a constant
    /// </summary>
    public class ConstTextFragment : IFragment
    {
        private string text;

        /// <summary>
        ///     Gets the fragment type
        /// </summary>
        public FragmentType Type => FragmentType.Const;

        /// <summary>
        ///     Parses the section of the string to extract the type
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="source">source string</param>
        /// <param name="idxStart">index start</param>
        /// <param name="length">maximum length to parse</param>
        /// <returns>index to start next parse at</returns>
        public int Parse(
            IContext context,
            string source,
            int idxStart,
            int length)
        {
            if (this.text != null)
            {
                throw new InvalidOperationException("object has already been initialized");
            }

            this.text = source
                .Substring(idxStart, length)
                .Replace("\\[\\[\\<", "[[<")
                .Replace("\\>\\]\\]", ">]]");

            return idxStart + length;
        }

        /// <summary>
        ///     Gets the set of variables referenced by the template
        /// </summary>
        /// <returns>resulting value</returns>
        public ICollection<string> GetVariables()
        {
            return ListHelper.EmptyList<string>();
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
            if (this.text == null)
            {
                throw new InvalidOperationException("object has not yet been initialized");
            }

            target = target ?? new StringBuilder();
            target.Append(this.text);
            return target;
        }

        /// <summary>
        ///     returns a string that represents this instance
        /// </summary>
        /// <returns>string that represents this instance</returns>
        public override string ToString()
        {
            return "[CONST text:'" + this.text + "']";
        }
    }
}
