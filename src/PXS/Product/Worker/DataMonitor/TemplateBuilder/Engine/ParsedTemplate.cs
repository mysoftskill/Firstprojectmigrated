// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.TemplateBuilder.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.Common.TemplateBuilder.Fragments;

    /// <summary>
    ///     represents a parsed template
    /// </summary>
    public class ParsedTemplate : IParsedTemplate
    {
        private readonly IModelManipulator modelManipulator;
        private readonly IFragment rootFragment;

        /// <summary>
        ///     Initializes a new instance of the ParsedTemplate class
        /// </summary>
        /// <param name="modelManipulator">model manipulator</param>
        /// <param name="def">template definition</param>
        /// <param name="rootFragment">parsed template root fragment</param>
        public ParsedTemplate(
            IModelManipulator modelManipulator,
            TemplateDef def,
            IFragment rootFragment)
        {
            ArgumentCheck.ThrowIfNull(def, nameof(def));

            this.modelManipulator = modelManipulator ?? throw new ArgumentNullException(nameof(modelManipulator));
            this.rootFragment = rootFragment ?? throw new ArgumentNullException(nameof(rootFragment));

            this.Tag = ArgumentCheck.ReturnIfNotNullEmptyOrWhiteSpace(def.Tag, nameof(def) + ".Tag");
        }

        /// <summary>
        ///     Gets the template tag
        /// </summary>
        public string Tag { get; }

        /// <summary>
        ///     Gets the set of variables referenced by the template
        /// </summary>
        /// <returns>resulting value</returns>
        public ICollection<string> GetVariables() => this.rootFragment.GetVariables();

        /// <summary>
        ///     Renders a text blob by applying the supplied data to the template
        /// </summary>
        /// <param name="context">render context</param>
        /// <param name="parameters">template parameters</param>
        /// <param name="model">collection of data items</param>
        /// <returns>rendered text</returns>
        public string Render(
            IContext context,
            ICollection<KeyValuePair<string, ModelValue>> parameters,
            object model)
        {
            object renderModel;

            if (this.rootFragment == null)
            {
                throw new InvalidOperationException("object has already been initialized");
            }

            renderModel = this.modelManipulator.MergeModels(context, model, null, parameters);

            return this.rootFragment.Render(context, new StringBuilder(), renderModel).ToString();
        }
    }
}
