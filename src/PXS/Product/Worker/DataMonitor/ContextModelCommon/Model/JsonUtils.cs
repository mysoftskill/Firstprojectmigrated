// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.DataModel
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.Exceptions;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using StringExtensions = Microsoft.Oss.Membership.CommonCore.Extensions.StringExtensions;

    /// <summary>
    ///     utilities for manipulating JSON models
    /// </summary>
    public class JsonUtils
    {
        private static readonly char[] DotOrQuote = { '.', '"' };
        private static readonly char[] Quote = { '"' };
        private static readonly char[] Dot = { '.' };

        /// <summary>
        ///     extracts a collection of items from the model
        /// </summary>
        /// <param name="source">model to extract a value from</param>
        /// <param name="selector">path to value</param>
        /// <returns>true if the requested object could be found, false otherwise</returns>
        public static JArray ExtractCollection(
            JToken source,
            string selector)
        {
            try
            {
                return new JArray(source.SelectTokens(selector, false));
            }
            catch (JsonException e)
            {
                throw new InvalidPathException("the specified data element path [" + selector + "] is not supported", e);
            }
        }

        /// <summary>
        ///     determines whether the path is a simple path (non-quoted, single element path) that does not need parsing
        /// </summary>
        /// <param name="path">path</param>
        /// <returns>resulting value</returns>
        public static bool IsSingleElementNonQuotedPath(string path)
        {
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(path, nameof(path));
            return path.IndexOfAny(JsonUtils.DotOrQuote) < 0;
        }

        /// <summary>
        ///     Parses the object path into a collection of path elements
        /// </summary>
        /// <param name="path">path expression</param>
        /// <returns>resulting value</returns>
        /// <remarks>
        ///     this utility method supports only the dot syntax (including quoted path elements). It does not support the
        ///      array or dictionary-like notation
        ///     this utility method also does not support escaping quotation marks in property names
        /// </remarks>
        public static IList<string> ParsePath(string path)
        {
            List<string> names = new List<string>();

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new InvalidPathException("at least one path element must be present");
            }

            path = path.Trim();

            for (int idx = 0; idx < path.Length;)
            {
                char[] terminating = JsonUtils.Dot;
                string name;
                bool isQuoted = false;
                int idxTokenStart;

                if (path[idx] == '"')
                {
                    terminating = JsonUtils.Quote;
                    isQuoted = true;

                    // skip past the opening quote 
                    ++idx;
                }
                
                idxTokenStart = idx;

                // this intentionally does not support escaped quotes inside the path element name
                idx = path.IndexOfAny(terminating, idx);
                if (idx < 0)
                {
                    if (isQuoted)
                    {
                        throw new InvalidPathException("path contains unterminated quote");
                    }

                    idx = path.Length;
                }

                if (isQuoted)
                {
                    if ((idx - idxTokenStart) <= 0)
                    {
                        throw new InvalidPathException("path elements may not be zero-length");
                    }

                    // grab the path element from right after the opening quote to right before the closing quote. All characters
                    //  within this range are considered significant.
                    name = path.Substring(idxTokenStart, idx - idxTokenStart);

                    // move beyond the closing quote and find the next non-whitespace character. We should either hit a '.' as
                    //  the first non-whitespace character or the end of the path
                    // ReSharper disable once EmptyEmbeddedStatement
                    for (++idx; idx < path.Length && char.IsWhiteSpace(path[idx]); ++idx) ;

                    // if we didn't hit the end of path, then the character we're on has to be a '.'
                    if (idx < path.Length)
                    {
                        if (path[idx] != '.')
                        {
                            throw new InvalidPathException(
                                "a path separator or end of path is required after a quoted path element");
                        }
                    }

                    // intentionally do not trim as whitespace within the quotes is considered significant and must be considered
                    //  part of the path element name
                }
                else
                {
                    name = (idx - idxTokenStart) > 0 ? 
                        path.Substring(idxTokenStart, idx - idxTokenStart).Trim() : 
                        null;

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        throw new InvalidPathException(
                            "path elements may not be zero-length (or empty without using a quoted path element)");
                    }

                    if (name.Any(char.IsWhiteSpace))
                    {
                        throw new InvalidPathException("unquoted path elements may not contain whitespace");
                    }
                }

                names.Add(name);

                // if we see a '.' then find the next whitespace character after the dot.  If we're at end of line, just continue on and 
                //  let the loop condition check terminate the loop.  The checks above should ensure that we're either on a '.' or the 
                //  end of path once we hit this code

                if (idx < path.Length)
                {
                    // skip past the '.' we are on and find the first non-whitespace character in this path element
                    // ReSharper disable once EmptyEmbeddedStatement
                    for (++idx; idx < path.Length && char.IsWhiteSpace(path[idx]); ++idx) ;

                    if (idx >= path.Length)
                    {
                        throw new InvalidPathException("path elements may not be zero-length");
                    }
                }
            }

            // the root is implied, so ignore it if the caller specified it
            if (names.Count > 0 && names[0].Equals("$"))
            {
                names.RemoveAt(0);
            }

            if (names.Count == 0)
            {
                throw new InvalidPathException("at least one path element must be present");
            }

            return names;
        }

        /// <summary>
        ///     Gets the container object that the property should be added to / updated and the name of the leaf element
        /// </summary>
        /// <param name="context">model context</param>
        /// <param name="root">root objects</param>
        /// <param name="pathList">list of sub-path container elements</param>
        /// <returns>tuple that is the container to write to and the leaf property name</returns>
        public static (JObject Container, string LeafPropName) GetContainerAndLeafPropName(
            IContext context,
            JObject root,
            IList<string> pathList)
        {
            string leaf;
            int i;

            ArgumentCheck.ThrowIfNull(pathList, nameof(pathList));
            ArgumentCheck.ThrowIfNull(context, nameof(context));

            leaf = pathList.Last();

            root = root ?? new JObject();

            for (i = 0; i < pathList.Count - 1; ++i)
            {
                JProperty prop = root.Property(pathList[i]);
                JObject nextRoot;

                nextRoot = prop?.Value as JObject;
                if (nextRoot != null)
                {
                    root = nextRoot;
                    continue;
                }

                if (prop?.Value != null && prop.Value.Type != JTokenType.Null)
                {
                    throw new InvalidPathException(
                        StringExtensions.FormatInvariant(
                            "Intermediate path elements must be objects. [{0}] is a [{1}]",
                            string.Join(".", pathList.Take(i)),
                            prop.Value.Type.ToString()));
                }

                context.LogVerbose(
                    "Adding intermediate paths [{0}] to object path [{1}]".FormatInvariant(
                        string.Join(",", pathList.Skip(i).Take(pathList.Count - i - 1)),
                        string.Join(",", pathList.Skip(i - 1))));

                nextRoot = new JObject();

                if (prop != null)
                {
                    prop.Value = nextRoot;
                }
                else
                {
                    root.Add(pathList[i], nextRoot);
                }

                root = nextRoot;
                ++i;
                break;
            }

            for (; i < pathList.Count - 1; ++i)
            {
                JObject nextRoot = new JObject();
                root.Add(pathList[i], nextRoot);
                root = nextRoot;
            }

            return (root, leaf);
        }
    }
}
