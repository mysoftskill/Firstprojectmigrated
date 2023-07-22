// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.TemplateBuilder.Fragments
{
    using System.Collections.Generic;
    using System.Text;

    using Microsoft.PrivacyServices.Common.Context;

    /// <summary>
    ///     a list of valid fragment types
    /// </summary>
    public enum FragmentType
    {
        /// <summary>
        ///     invalid option (intentionally set to 0 so it is the default)
        /// </summary>
        Invalid = 0,

        /// <summary>
        ///     indicates a constant text fragment
        /// </summary>
        Const,

        /// <summary>
        ///     indicates a replacement variable fragment
        /// </summary>
        Variable,

        /// <summary>
        ///     indicates a foreach array iterator fragment
        /// </summary>
        Foreach,

        /// <summary>
        ///     indicates a frangment that is itself a template
        /// </summary>
        /// <remarks>
        ///     a template fragment could live inside an operation fragment such as a Foreach fragment, where it represents the
        ///      template to be added for each item in the source loop
        /// </remarks>
        Template,
    }

    /// <summary>
    ///     contract for template fragment classes
    /// </summary>
    public interface IFragment
    {
        /// <summary>
        ///     Gets the fragment type
        /// </summary>
        FragmentType Type { get; }

        /// <summary>
        ///     Parses the section of the string to extract the type
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="source">source string</param>
        /// <param name="idxStart">index start</param>
        /// <param name="length">maximum length to parse</param>
        /// <returns>index to start next parse at</returns>
        int Parse(
            IContext context,
            string source,
            int idxStart,
            int length);

        /// <summary>
        ///     Gets the set of variables referenced by the template
        /// </summary>
        /// <returns>resulting value</returns>
        ICollection<string> GetVariables();

        /// <summary>
        ///     Applies the specified source
        /// </summary>
        /// <param name="context">render context</param>
        /// <param name="target">target StringBuilder to write to. Can be null to generate a new builder</param>
        /// <param name="model">data to be used for replacement variables</param>
        /// <returns>source or a created StringBuilder if target is null</returns>
        StringBuilder Render(
            IContext context,
            StringBuilder target,
            object model);
    }
}
