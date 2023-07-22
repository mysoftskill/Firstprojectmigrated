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

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    ///     implements a factory creating models based on Json.NET JTokens
    /// </summary>
    public class JsonNetModelManipulator : 
        IModelReader,
        IModelWriter
    {
        /// <summary>
        ///     Creates a new empty model object
        /// </summary>
        /// <returns>new model object</returns>
        public object CreateEmpty()
        {
            return new JObject();
        }

        /// <summary>
        ///     transform the source object into an instance of the model 
        /// </summary>
        /// <param name="source">source object</param>
        /// <returns>new model object</returns>
        public object TransformFrom(object source)
        {
            return this.InternalTransform(source);
        }

        /// <summary>
        ///     transform the source object into an instance of the requested type
        /// </summary>
        /// <param name="source">source object</param>
        /// <returns>instance of requested object type</returns>
        public T TransformTo<T>(object source)
        {
            JToken tokenObj;

            if (source == null)
            {
                return default(T);
            }

            if (source.GetType() == typeof(T))
            {
                return (T)source;
            }

            tokenObj = this.InternalTransform(source);

            return tokenObj.ToObject<T>();
        }

        /// <summary>
        ///     transform the source object into an enumerable collection
        /// </summary>
        /// <param name="source">source object</param>
        /// <returns>new model object</returns>
        public IEnumerable ToEnumerable(object source)
        {
            JToken objSrc = this.InternalTransform(source);
            return (objSrc is JArray arrayObj) ? arrayObj : new JArray(objSrc);
        }

        /// <summary>
        ///     adds a submodel item to the existing model
        /// </summary>
        /// <param name="context">model context</param>
        /// <param name="target">model to add the submodel to</param>
        /// <param name="path">name that will be used to reference the submodel within the model</param>
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
            JProperty prop;
            JObject objDest = this.InternalTransform(target) as JObject;
            JToken tokenSrc = this.InternalTransform(submodel);
            string propName;
            bool added = false;

            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(path, nameof(path));

            if (tokenSrc == null || objDest == null)
            {
                string subModelType = submodel?.GetType().FullName ?? "UNKNOWN";
                string targetType = target?.GetType().FullName ?? "UNKNOWN";
                throw new InvalidPathException($"Cannot add a submodel [{subModelType}] object into a model [{targetType}] object");
            }

            if (JsonUtils.IsSingleElementNonQuotedPath(path))
            {
                propName = path?.Trim();

                if (string.IsNullOrWhiteSpace(propName))
                {
                    throw new InvalidPathException("must specify a valid property path");
                }
            }
            else
            {
                IList<string> pathList = JsonUtils.ParsePath(path);
                (objDest, propName) = JsonUtils.GetContainerAndLeafPropName(context, objDest, pathList);
            }

            prop = objDest.Property(propName);
            
            if (prop == null)
            {
                context.LogVerbose($"adding property [{propName}] => [{objDest.Path}]");
                prop = new JProperty(propName, null);
                objDest.Add(prop);
                added = true;
            }

            // the array add modes always result in the value being an array.  
            //  Step 1: get a target array to write to
            //    if there is no existing value, an empty array is constructed
            //    if there is an existing array value, it is used as is
            //    if there is an existing non-array value, it is first converted to a single element array containing that existing
            //      value
            //  Step 2: add the input item to the array from step 1
            //    if the input value is not an array, the two array modes behave the same: the object is added to the array from
            //      step 1
            //    if the input value is an array and the mode is ArrayAdd, the input array is added as-is to the array from step
            //      1 (i.e. we get an array of arrays)
            //    if th einput value is an array and the mode is ArrayUnion, the input array is enumerated and all of it's elements
            //      are added individually to the array from step 1. Note that this is not recursive, so if the input array is itself
            //      an array of arrays, those inner arrays are treated as opaque objects and not further expanded.
            if (mode != MergeMode.ReplaceExisting)
            {
                JArray newValue = prop.Value as JArray;
                    
                if (newValue == null)
                {
                    newValue = prop.Value != null && prop.Value.Type != JTokenType.Null ? new JArray(prop.Value) : new JArray();

                    if (added == false)
                    {
                        context.LogVerbose(
                            $"converting existing item to {newValue.Count} element array [{objDest.Path}{propName}] for {mode}");
                    }
                }
                else
                {
                    context.LogVerbose($"adding to existing array [{objDest.Path}{propName}] for {mode}");
                }

                if (mode == MergeMode.ArrayAdd || tokenSrc is JArray == false)
                {
                    newValue.Add(tokenSrc);
                }
                else
                {
                    foreach (JToken t in tokenSrc as JArray)
                    {
                        newValue.Add(t);
                    }
                }

                tokenSrc = newValue;
            }

            // replace existing is a simple overwrite of existing value
            else if (added == false)
            {
                context.LogVerbose($"overwriting property [{objDest.Path}{propName}]");
            }

            prop.Value = tokenSrc;

            return objDest;
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
            IModelReader reader,
            object source,
            object target,
            ICollection<KeyValuePair<string, T>> transform)
            where T : ModelValue
        {
            JObject dest = this.InternalTransform(target) as JObject;
            JObject src = this.InternalTransform(source) as JObject;

            if (dest == null && target != null)
            {
                throw new ArgumentException($"Cannot merge into a submodel of type {target.GetType().FullName}", nameof(target));
            }

            if (src == null && source != null)
            {
                throw new ArgumentException($"Cannot merge from a submodel of type {source.GetType().FullName}", nameof(target));
            }

            dest = dest ?? new JObject();

            // if we have a null or empty input object or no transform instructions, no need to do any more work
            if (src == null || 
                transform == null || transform.Count == 0)
            {
                return dest;
            }

            // if we were not passed a reader, then use the current class
            reader = reader ?? this;

            foreach (KeyValuePair<string, T> xform in transform)
            {
                ModelValue modelVal = xform.Value;
                string destName = xform.Key.Trim();
                JToken value = null;

                if (modelVal == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(modelVal.SelectMany) == false)
                {
                    JArray collection = JsonUtils.ExtractCollection(src, modelVal.SelectMany);

                    if (collection.Count > 0 || modelVal.Const == null)
                    {
                        value = collection;
                        context.LogVerbose(
                            $"[{modelVal.SelectMany}] produced a {collection.Count} item collection ==> [{destName}]");
                    }
                    else
                    {
                        value = this.InternalTransform(modelVal.Const);
                        context.LogVerbose(
                            $"[{modelVal.SelectMany}] produced a 0 item collection, using Const ==> [{destName}]");
                    }

                }
                else
                {
                    string select = modelVal.Select?.Trim();
                    bool foundValue = false;
                    bool hasSelect = string.IsNullOrWhiteSpace(select) == false;

                    if (hasSelect)
                    {
                        object selectValue;

                        foundValue = reader.TryExtractValue(context, src, select, null, out selectValue);

                        value = selectValue as JToken;

                        if (selectValue != null && value == null)
                        {
                            value = new JValue(selectValue);
                        }
                    }

                    if (foundValue)
                    {
                        context.LogVerbose(
                            $"[{select}] produced a [{value?.Type.ToString() ?? "<UNKNOWN>"}] value ==> [{destName}]");
                    }
                    else if (modelVal.Const != null)
                    {
                        value = this.InternalTransform(modelVal.Const);

                        context.LogVerbose(
                            hasSelect ?
                                $"[{select}] was not found. Constant produced a [{value.Type}] value ==> [{destName}]" :
                                $"No select. Constant produced a [{value.Type}] value ==> [{destName}]");
                    }
                    else
                    {
                        context.LogVerbose(
                            hasSelect ?
                                $"[{select}] was not found and no constant . No value will be populated for [{destName}]" :
                                $"No select or constant . No value will be populated for [{destName}]");

                        continue;
                    }

                }

                this.AddSubmodel(context, dest, destName, value, modelVal.MergeMode);
            }

            return dest;
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
            return this.MergeModels(context, this, source, target, transform);
        }

        /// <summary>
        ///     removes a submodel from the existing model
        /// </summary>
        /// <param name="target">model to add the submodel to</param>
        /// <param name="path">property name that will be used to reference the submodel</param>
        /// <returns>resulting model</returns>
        public object RemoveSubmodel(
            object target,
            string path)
        {
            JObject objDest;
            JObject objRoot;
            string propName;

            path = path?.Trim();

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new InvalidPathException("path may not be empty or null");
            }

            objDest = objRoot = this.InternalTransform(target) as JObject;
            if (objRoot == null)
            {
                string targetType = target?.GetType().FullName ?? "UNKNOWN";
                throw new InvalidPathException("Cannot remove a submodel from a [" + targetType + "] object");
            }

            if (JsonUtils.IsSingleElementNonQuotedPath(path))
            {
                propName = path;
            }
            else
            {
                IList<string> pathList = JsonUtils.ParsePath(path);
                if (pathList.Count == 0)
                {
                    throw new InvalidPathException("path may not be empty or null");
                }

                if (pathList.Count > 1)
                {
                    JToken tokenDest;
                    try
                    {
                        tokenDest = objRoot.SelectToken(string.Join(".", pathList.Take(pathList.Count - 1)));
                    }
                    catch (JsonException e)
                    {
                        throw new InvalidPathException("a valid path must be specified", e);
                    }

                    // if the intermediate element doesn't exist, then the intention of the remove request was pre-satisfied, so
                    //  just return
                    if (tokenDest == null)
                    {
                        return objRoot;
                    }

                    objDest = tokenDest as JObject;
                    if (objDest == null)
                    {
                        string targetType = tokenDest.GetType().FullName ?? "UNKNOWN";
                        throw new InvalidPathException("Cannot remove a submodel from a [" + targetType + "] object");
                    }
                }

                propName = pathList[pathList.Count - 1];
            }

            objDest.Remove(propName);

            return objRoot;
        }

        /// <summary>
        ///     extracts a value from the model
        /// </summary>
        /// <typeparam name="T">type of expected result</typeparam>
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
            JToken objSrc = this.InternalTransform(source);
            JToken localResult = null;

            if (source != null)
            {
                try
                {
                    localResult = objSrc.SelectToken(selector, false);
                }
                catch (JsonException e)
                {
                    throw new InvalidPathException("the specified data element path [" + selector + "] is not supported", e);
                }
            }

            result = localResult != null ?
                (typeof(T) == typeof(object) ? (T)(object)localResult : localResult.ToObject<T>()) : 
                defaultValue;
           
            return localResult != null;
        }

        /// <summary>
        ///     transforms the source object into a JToken
        /// </summary>
        /// <param name="source">source object</param>
        /// <returns>resulting value</returns>
        private JToken InternalTransform(object source)
        {
            return source == null ?
                null :
                source is JToken token ? 
                    token : 
                    JToken.FromObject(source);
        }
    }
}
