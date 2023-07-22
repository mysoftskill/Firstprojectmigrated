// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.TemplateBuilder.Fragments
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.Common.Context;

    /// <summary>
    ///     PropResponse enum
    /// </summary>
    public enum PropResponse
    {
        /// <summary>
        ///     invalid option
        /// </summary>
        Invalid = 0,

        /// <summary>
        ///     property has been set
        /// </summary>
        Ok,

        /// <summary>
        ///     property with supplied name is not supported
        /// </summary>
        NotSupported,

        /// <summary>
        ///     property value is in a bad format
        /// </summary>
        BadFormat,

        /// <summary>
        ///     property has already been set and multiple instances are not supported
        /// </summary>
        Duplicate,
    }

    /// <summary>
    ///     template fragment that is a replacement variable
    /// </summary>
    public abstract class OpFragment : IFragment
    {
        private const char EscapeChar = '\\';

        static readonly HashSet<char> AllowedPropNameChars = 
            new HashSet<char>("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-+._");

        /// <summary>
        ///     Gets the fragment type
        /// </summary>
        public abstract FragmentType Type { get; }

        /// <summary>
        ///     Gets prefix
        /// </summary>
        protected abstract string Prefix { get; }

        /// <summary>
        ///     Gets suffix
        /// </summary>
        protected abstract string Suffix { get; }

        /// <summary>
        ///     Parses the tag
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="source">source string to parse</param>
        /// <param name="idxStart">index start</param>
        /// <param name="length">maximum length</param>
        /// <returns>resulting value</returns>
        public abstract int Parse(
            IContext context,
            string source,
            int idxStart,
            int length);

        /// <summary>
        ///     Gets the set of variables referenced by the template
        /// </summary>
        /// <returns>resulting value</returns>
        public abstract ICollection<string> GetVariables();

        /// <summary>
        ///     Applies the specified model to the template fragment
        /// </summary>
        /// <param name="context">render context</param>
        /// <param name="target">target StringBuilder to write to. Can be null to generate a new builder</param>
        /// <param name="model">data to be used for replacement variables</param>
        /// <returns>source or a created StringBuilder if target is null</returns>
        public abstract StringBuilder Render(
            IContext context,
            StringBuilder target,
            object model);

        /// <summary>
        ///     sets a found property on the derived object
        /// </summary>
        /// <param name="name">property name</param>
        /// <param name="value">property value</param>
        /// <returns>false if the property is not valid or supported</returns>
        protected abstract PropResponse SetPropery(
            string name,
            string value);

        /// <summary>
        ///     Verifies all requried properties are present
        /// </summary>
        /// <returns>collection of missing properties (or an empty enumerable if none)</returns>
        protected abstract IEnumerable<string> VerifyRequiredProperties();

        /// <summary>
        ///     Parses the section of the string to extract the type
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="source">source string</param>
        /// <param name="idxStart">index start</param>
        /// <param name="length">length to parse</param>
        /// <returns>index to start next parse at</returns>
        protected (int IdxNext, int LengthRemaining) ParseTag(
            string context,
            string source, 
            int idxStart, 
            int length)
        {
            ICollection<string> missingNames;
            int idxEnd;
            int idx = idxStart;
            int len = length;
            
            if (len < this.Prefix.Length || string.CompareOrdinal(source, idx, this.Prefix, 0, this.Prefix.Length) != 0)
            {
                throw new TemplateParseException(
                    context + " does not start with expected prefix sequence " + this.Prefix);
            }

            idx += this.Prefix.Length;
            len -= this.Prefix.Length;

            idxEnd = source.IndexOf(this.Suffix, idx, len, StringComparison.Ordinal);
            if (idxEnd < 0)
            {
                throw new TemplateParseException(context + " is missing end sequence " + this.Suffix);
            }

            // do not include the end sequence
            len = idxEnd - idx;

            while (len > 0)
            {
                PropResponse response;
                string value;
                string name;

                OpFragment.SkipWhitespace(context, source, true, ref idx, ref len);
                if (len == 0)
                {
                    break;
                }

                name = OpFragment.ParseKey(context, source, ref idx, ref len);

                OpFragment.SkipWhitespace(context, source, false, ref idx, ref len);

                value = OpFragment.ParseValue(context, source, name, ref idx, ref len);

                response = this.SetPropery(name, value);
                if (response != PropResponse.Ok)
                {
                    if (response == PropResponse.BadFormat)
                    {
                        throw new TemplateParseException(
                            $"{context} has a property '{name}' with an incorrectly formatted value '{value}'");
                    }
                    else if (response == PropResponse.NotSupported)
                    {
                        throw new TemplateParseException(
                            $"{context} has a property '{name}' that is not a supported property name");
                    }
                    else if (response == PropResponse.Duplicate)
                    {
                        throw new TemplateParseException(
                            $"{context} has a multiple instances of property '{name}' when multiple instances are not supported");
                    }
                    else
                    {
                        throw new TemplateParseException(
                            $"{context} has a property '{name}' with a '{value}' that is invalid");
                    }
                }
            }

            missingNames = this.VerifyRequiredProperties()?.ToList();
            if (missingNames?.Count > 0)
            {
                throw new TemplateParseException(
                    context + " is missing required properties " + string.Join(", ", missingNames));
            }

            idxEnd += this.Suffix.Length;

            return (idxEnd, length - (idxEnd - idxStart));
        }

        /// <summary>
        ///     Skips whitespace and returns the new length and index
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="source">source string</param>
        /// <param name="isReachingLenOk">
        ///     true if hitting the length before running out of whitespace is ok; false to throw in that case
        /// </param>
        /// <param name="idx">index in source</param>
        /// <param name="len">length of source to allow parsing in</param>
        private static void SkipWhitespace(
            string context,
            string source,
            bool isReachingLenOk,
            ref int idx,
            ref int len)
        {
            int idxStart = idx;

            // ReSharper disable once EmptyEmbeddedStatement
            for (; len > 0 && char.IsWhiteSpace(source[idx]); ++idx, --len);

            if (len == 0 && isReachingLenOk == false)
            {
                throw new TemplateParseException(
                    context + " could not find non-whitespace after position " + idxStart.ToStringInvariant());
            }
        }

        /// <summary>
        ///     Parses a key containing a limited set of characters and terminated with a ':'
        /// </summary>
        /// <param name="context">context</param>
        /// <param name="source">source</param>
        /// <param name="idx">index</param>
        /// <param name="len">length</param>
        /// <returns>resulting value</returns>
        private static string ParseKey(
            string context,
            string source,
            ref int idx,
            ref int len)
        {
            int idxStart = idx;
            int idxEnd;

            // ReSharper disable once EmptyEmbeddedStatement
            for (; len > 0 && OpFragment.AllowedPropNameChars.Contains(source[idx]); ++idx, --len);

            idxEnd = idx;

            if (idxEnd - idxStart == 0)
            {
                throw new TemplateParseException(
                    context + " empty property name after position" + idxStart.ToStringInvariant());
            }

            OpFragment.SkipWhitespace(context, source, false, ref idx, ref len);

            if (len == 0 || source[idx] != ':')
            {
                throw new TemplateParseException(
                    context + " invalid property name after position" + idxStart.ToStringInvariant());
            }

            // move over the ':'
            ++idx;
            --len;

            return source.Substring(idxStart, idxEnd - idxStart);
        }

        /// <summary>
        ///     Parses a value that is either surrounded by single quotes, double quotes, or whitespace delimited
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="source">source string</param>
        /// <param name="name">property name that the value is associated with</param>
        /// <param name="idx">index in source</param>
        /// <param name="len">length of source to allow parsing in</param>
        /// <returns>resulting value</returns>
        private static string ParseValue(
            string context,
            string source,
            string name,
            ref int idx,
            ref int len)
        {
            const char LookForWhitespace = '\0';

            char GetTerminating(char start)
            {
                switch (start)
                {
                    case '\'': return '\'';
                    case '"': return '"';
                    default: return LookForWhitespace;
                }
            }

            char terminating = GetTerminating(source[idx]);
            int idxStart;

            if (terminating != LookForWhitespace)
            {
                char current;
                char prev;
                int idxEnd;

                // skip over the opening character
                ++idx;
                --len;

                idxStart = idx;

                // look for the terminating character
                for (;;)
                {
                    // ReSharper disable once EmptyEmbeddedStatement
                    for (; len > 0 && source[idx] != terminating; ++idx, --len) ;

                    current = source[idx];
                    prev = source[idx - 1];

                    // if we hit a terminating character, but the previous character is the escape sequence, then consider
                    //  it part of the string (though if we're at the length limit, bail regardless)
                    if (len == 0 || prev != OpFragment.EscapeChar)
                    {
                        break;
                    }

                    ++idx;
                    --len;
                }

                idxEnd = idx;

                if (current != terminating ||
                    (current == terminating && prev == OpFragment.EscapeChar && len == 0) ||
                    idxEnd - idxStart == 0)
                {
                    throw new TemplateParseException(
                        $"{context} value for name {name} expected to be terminated with '{terminating}'");
                }

                // we're on the end symbol, so move the next index up one
                ++idx;
                --len;

                return source
                    .Substring(idxStart, idxEnd - idxStart)
                    .Replace("\\\"", "\"")
                    .Replace("\\\'", "\'")
                    .Replace("\\\\", "\\");
            }
            else
            {
                idxStart = idx;

                // ReSharper disable once EmptyEmbeddedStatement
                for (; len > 0 && char.IsWhiteSpace(source[idx]) == false; ++idx, --len) ;

                if (idx - idxStart == 0)
                {
                    throw new TemplateParseException($"{context}: value for {name} was found to be empty");
                }

                return source.Substring(idxStart, idx - idxStart);
            }
        }
    }
}
