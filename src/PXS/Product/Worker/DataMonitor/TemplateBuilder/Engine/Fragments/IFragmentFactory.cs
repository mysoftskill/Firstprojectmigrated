// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.TemplateBuilder.Fragments
{
    /// <summary>
    ///     contract for object that create new template fragments
    /// </summary>
    public interface IFragmentFactory
    {
        /// <summary>
        ///     Creates a constant text fragment
        /// </summary>
        /// <returns>resulting value</returns>
        IFragment CreateConstFragment();

        /// <summary>
        ///     Creates a template fragment
        /// </summary>
        /// <returns>resulting value</returns>
        IFragment CreateTemplateFragment();

        /// <summary>
        ///     Creates an operation fragment
        /// </summary>
        /// <param name="prefixSequence">prefix sequence</param>
        /// <returns>resulting value</returns>
        IFragment CreateOpFragment(string prefixSequence);
    }
}
