// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.TemplateBuilder
{
    using System;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.Common.TemplateBuilder.Engine;
    using Microsoft.PrivacyServices.Common.TemplateBuilder.Fragments;

    /// <summary>
    ///     parses a template
    /// </summary>
    public class TemplateParser : ITemplateParser
    {
        private readonly IModelManipulator modelManipulator;
        private readonly IFragmentFactory fragFactory;

        /// <summary>
        ///     Initializes a new instance of the TemplateParser class
        /// </summary>
        /// <param name="modelManipulator">Model manipulator</param>
        /// <param name="fragFactory">frag factory</param>
        public TemplateParser(
            IModelManipulator modelManipulator,
            IFragmentFactory fragFactory)
        {
            this.modelManipulator = modelManipulator ?? throw new ArgumentNullException(nameof(modelManipulator));
            this.fragFactory = fragFactory ?? throw new ArgumentNullException(nameof(fragFactory));
        }

        /// <summary>
        ///     Parses the specified template and returns a parsed template object
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="def">template definition to parse</param>
        /// <returns>parsed template object</returns>
        public IParsedTemplate Parse(
            IContext context,
            TemplateDef def)
        {
            IFragment root = this.fragFactory.CreateTemplateFragment();

            root.Parse(context, def.Text, 0, def.Text.Length);

            return new ParsedTemplate(this.modelManipulator, def, root);
        }
    }
}
