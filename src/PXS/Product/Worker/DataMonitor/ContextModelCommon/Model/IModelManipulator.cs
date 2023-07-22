// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.DataModel
{
    using System.Collections;
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.Common.Context;

    /// <summary>
    ///     instructions for how to merge a particular value between model instances
    /// </summary>
    public enum MergeMode
    {
        /// <summary>
        ///     the existing node is replaced with the new value always
        /// </summary>
        /// <remarks>this is the default</remarks>
        ReplaceExisting = 0,

        /// <summary>
        ///     adds the input object into an existing or newly created array
        /// </summary>
        /// <remarks>
        ///     if the input item is an array, it adds the input array as a subarray of the existing one and does not merge
        ///      contents (allowing one to build an array of arrays)
        ///     if the existing item is not an array, it transforms it into an array containing the existing item
        ///     if there is no existing item, a new array is created
        /// </remarks>
        ArrayAdd = 1,

        /// <summary>
        ///     merges the input object into an existing or newly created array
        /// </summary>
        /// <remarks>
        ///     if the input object is an array, it merges the input array into the existing array (duplicate detection is NOT
        ///       performed)
        ///     if the existing item is not an array, it transforms it into an array containing the existing item
        ///     if there is no existing item, a new array is created
        /// </remarks>
        ArrayUnion = 2,
    }

    /// <summary>
    ///     contract for objects reading from models
    /// </summary>
    public interface IModelReader
    {
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
        bool TryExtractValue<T>(
            IContext context,
            object source,
            string selector,
            T defaultValue,
            out T result);
    }

    /// <summary>
    ///     contract for objects to create new models
    /// </summary>
    public interface IModelWriter
    {
        /// <summary>
        ///     Creates a new empty model object
        /// </summary>
        /// <returns>new model object</returns>
        object CreateEmpty();

        /// <summary>
        ///     transform the source object into an instance of the model 
        /// </summary>
        /// <param name="source">source object</param>
        /// <returns>new model object</returns>
        object TransformFrom(object source);

        /// <summary>
        ///     transform the source object into an instance of the requested type
        /// </summary>
        /// <param name="source">source object</param>
        /// <returns>instance of requested object type</returns>
        T TransformTo<T>(object source);

        /// <summary>
        ///     transform the source object into an enumerable collection
        /// </summary>
        /// <param name="source">source object</param>
        /// <returns>new model object</returns>
        IEnumerable ToEnumerable(object source);

        /// <summary>
        ///     adds a submodel item to the existing model
        /// </summary>
        /// <param name="context">model context</param>
        /// <param name="target">model to add the submodel to</param>
        /// <param name="path">property name that will be used to reference the submodel</param>
        /// <param name="submodel">submodel to add</param>
        /// <param name="mode">options to control the add</param>
        /// <returns>resulting model</returns>
        object AddSubmodel(
            IContext context,
            object target,
            string path,
            object submodel,
            MergeMode mode);

        /// <summary>
        ///     removes a submodel from the existing model
        /// </summary>
        /// <param name="target">model to add the submodel to</param>
        /// <param name="path">property name that will be used to reference the submodel</param>
        /// <returns>resulting model</returns>
        object RemoveSubmodel(
            object target,
            string path);

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
        object MergeModels<T>(
            IContext context,
            IModelReader reader,
            object source,
            object target,
            ICollection<KeyValuePair<string, T>> transform)
            where T : ModelValue;

        /// <summary>
        ///     Merges the models using the provided transform
        /// </summary>
        /// <param name="context">model context</param>
        /// <param name="source">source model</param>
        /// <param name="target">target model</param>
        /// <param name="transform">
        ///     a list of property names for the target model and a selector to read from the source or a constant value to add to
        ///      the targer
        ///     a null or empty value will copy nothing from the source model and produce an empty object
        /// </param>
        /// <returns>resulting value</returns>
        object MergeModels<T>(
            IContext context,
            object source,
            object target,
            ICollection<KeyValuePair<string, T>> transform)
            where T : ModelValue;
    }

    /// <summary>
    ///     contract for objects to create new models
    /// </summary>
    public interface IModelManipulator :
        IModelWriter,
        IModelReader
    {
        /// <summary>
        ///     extracts a value from the model
        /// </summary>
        /// <typeparam name="T">type of result value</typeparam>
        /// <param name="context">model context</param>
        /// <param name="source">model to extract a value from</param>
        /// <param name="selector">path to value</param>
        /// <param name="result">receives the resulting value if found</param>
        /// <returns>true if the requested object could be found, false otherwise</returns>
        bool TryExtractValue<T>(
            IContext context,
            object source,
            string selector,
            out T result);
    }
}
