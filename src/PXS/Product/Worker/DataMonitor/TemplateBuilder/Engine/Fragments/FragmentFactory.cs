// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.TemplateBuilder.Fragments
{
    using System;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Practices.Unity;

    /// <summary>
    ///     creates new fragment types
    /// </summary>
    public class FragmentFactory : IFragmentFactory
    {
        private readonly IUnityContainer creator;

        /// <summary>
        ///     Initializes a new instance of the FragmentFactory class
        /// </summary>
        /// <param name="creator">creator</param>
        public FragmentFactory(IUnityContainer creator)
        {
            this.creator = creator ?? throw new ArgumentNullException(nameof(creator));
        }

        /// <summary>
        ///     Creates a constant text fragment
        /// </summary>
        /// <returns>resulting value</returns>
        public IFragment CreateConstFragment()
        {
            return this.creator.Resolve<ConstTextFragment>();
        }

        /// <summary>
        ///     Creates a template fragment
        /// </summary>
        /// <returns>resulting value</returns>
        public IFragment CreateTemplateFragment()
        {
            return this.creator.Resolve<TemplateFragment>();
        }

        /// <summary>
        ///     Creates an operation fragment
        /// </summary>
        /// <param name="prefixSequence">prefix sequence</param>
        /// <returns>resulting value</returns>
        public IFragment CreateOpFragment(string prefixSequence)
        {
            return this.creator.Resolve<IFragment>(prefixSequence);
        }
    }
}
