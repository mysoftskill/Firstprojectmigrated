// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.DataModel
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.Exceptions;

    /// <summary>
    ///     model reader that can support reading from different model types
    /// </summary>
    public class AggregatingModelManipulator : IModelManipulator
    {
        private readonly IList<KeyValuePair<char, IModelReader>> readers;
        private readonly IModelReader defaultReader;

        private readonly IModelWriter writer;

        public AggregatingModelManipulator(
            IDictionary<char, IModelReader> readers,
            IModelReader defaultReader,
            IModelWriter writer)
        {
            this.defaultReader = defaultReader ?? throw new ArgumentNullException(nameof(defaultReader));
            this.writer = writer ?? throw new ArgumentNullException(nameof(writer));

            this.readers = readers?.Count > 0 ? readers.ToList() : null;
        }

        /// <summary>
        ///     Creates a new empty model object
        /// </summary>
        /// <returns>new model object</returns>
        public object CreateEmpty() => this.writer.CreateEmpty();

        /// <summary>
        ///     transform the source object into an instance of the model 
        /// </summary>
        /// <param name="source">source object</param>
        /// <returns>new model object</returns>
        public object TransformFrom(object source) => this.writer.TransformFrom(source);

        /// <summary>
        ///     transform the source object into an instance of the requested type
        /// </summary>
        /// <param name="source">source object</param>
        /// <returns>instance of requested object type</returns>
        public T TransformTo<T>(object source) => this.writer.TransformTo<T>(source);

        /// <summary>
        ///     transform the source object into an enumerable collection
        /// </summary>
        /// <param name="source">source object</param>
        /// <returns>new model object</returns>
        public IEnumerable ToEnumerable(object source) => this.writer.ToEnumerable(source);

        /// <summary>
        ///     adds a submodel item to the existing model
        /// </summary>
        /// <param name="context">model context</param>
        /// <param name="target">model to add the submodel to</param>
        /// <param name="path">property name that will be used to reference the submodel</param>
        /// <param name="submodel">submodel to add</param>
        /// <param name="mode">options to control the add</param>
        /// <returns>resulting model</returns>
        public object AddSubmodel(
            IContext context, 
            object target, 
            string path, 
            object submodel, 
            MergeMode mode)
        {
            return this.writer.AddSubmodel(context, target, path, submodel, mode);
        }

        /// <summary>
        ///     removes a submodel from the existing model
        /// </summary>
        /// <param name="target">model to add the submodel to</param>
        /// <param name="path">property name that will be used to reference the submodel</param>
        /// <returns>resulting model</returns>
        public object RemoveSubmodel(object target, string path)
        {
            return this.writer.RemoveSubmodel(target, path);
        }

        /// <summary>
        ///     Merges the models using the provided transform
        /// </summary>
        /// <param name="context">model context</param>
        /// <param name="reader">model reader to use to pull data out of the source model</param>
        /// <param name="source">source model</param>
        /// <param name="target">target model</param>
        /// <param name="transform">
        ///     a list of property names for the target model and a selector to read from the source or a constant value to add to
        ///      the targer
        ///     a null or empty value will copy nothing from the source model and produce an empty object
        /// </param>
        /// <returns>resulting value</returns>
        /// <remarks>
        ///     a missing value that appears in the transform but does not appear in the source model is silently ignored
        ///     if target is null, an empty model is created and populated with the result
        ///     if source is null, it is treated as an empty source model
        /// </remarks>
        public object MergeModels<T>(
            IContext context,
            IModelReader reader,
            object source, 
            object target, 
            ICollection<KeyValuePair<string, T>> transform)
            where T : ModelValue
        {
            return this.writer.MergeModels(context, reader, source, target, transform);
        }

        /// <summary>
        ///     Merges the models using the provided transform
        /// </summary>
        /// <param name="context">model context</param>
        /// <param name="source">source model</param>
        /// <param name="target">target model</param>
        /// <param name="transform">
        ///     a list of property names for the target model and a selector to read from the source or a constant value to add to
        ///     the target
        /// </param>
        /// <returns>resulting value</returns>
        /// <remarks>
        ///     a missing value that appears in the transform but does not appear in the source model is silently ignored
        ///     if target is null, an empty model is created and populated with the result
        ///     if source is null, it is treated as an empty source model
        /// </remarks>
        public object MergeModels<T>(
            IContext context,
            object source,
            object target,
            ICollection<KeyValuePair<string, T>> transform)
            where T : ModelValue
        {
            return this.writer.MergeModels(context, this, source, target, transform);
        }

        /// <summary>
        ///     extracts a value from the model
        /// </summary>
        /// <typeparam name="T">type of result value</typeparam>
        /// <param name="context">model context</param>
        /// <param name="source">model to extract a value from</param>
        /// <param name="selector">path to value</param>
        /// <param name="defaultValue">default value to assign if the value could not be found</param>
        /// <param name="result">receives the resulting value if found</param>
        /// <returns>true if the requested object could be found, false otherwise</returns>
        public bool TryExtractValue<T>(
            IContext context,
            object source,
            string selector,
            T defaultValue,
            out T result)
        {
            selector = selector?.Trim();

            if (string.IsNullOrEmpty(selector))
            {
                throw new InvalidPathException("the specified data element path [" + selector + "] is not supported");
            }

            if (this.readers != null)
            {
                for (int i = 0; i < this.readers.Count; ++i)
                {
                    if (selector.Length >= 2 && selector[0] == this.readers[i].Key && selector[1] == '.')
                    {
                        return this.readers[i].Value.TryExtractValue(context, source, selector, defaultValue, out result);
                    }
                }
            }

            return this.defaultReader.TryExtractValue(context, source, selector, defaultValue, out result);
        }

        /// <summary>
        ///     extracts a value from the model
        /// </summary>
        /// <typeparam name="T">type of result value</typeparam>
        /// <param name="context">model context</param>
        /// <param name="source">model to extract a value from</param>
        /// <param name="selector">path to value</param>
        /// <param name="result">receives the resulting value if found</param>
        /// <returns>true if the requested object could be found, false otherwise</returns>
        public bool TryExtractValue<T>(
            IContext context,
            object source,
            string selector,
            out T result)
        {
            return this.TryExtractValue(context, source, selector, default(T), out result);
        }
    }
}
