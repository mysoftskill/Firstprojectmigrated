// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace CosmosExportScanner
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>ConfigException class</summary>
    public class ConfigException : Exception
    {
        /// <summary>
        ///     Initializes a new instance of the ConfigException class
        /// </summary>
        /// <param name="name">name</param>
        public ConfigException(string name) :
            base($"argument [\"{name}\"] not specified and no default value found")
        {
        }
    }

    /// <summary>
    ///     Parameters class
    /// </summary>
    public class Parameters
    {
        /// <summary>Gets default arguments</summary>
        public IReadOnlyDictionary<string, ICollection<string>> Defaults { get; }

        /// <summary>Gets arguments</summary>
        public IReadOnlyDictionary<string, ICollection<string>> Args { get; }

        /// <summary>
        ///     Initializes a new instance of the Parameters class
        /// </summary>
        /// <param name="defaults">default values</param>
        /// <param name="args">arguments</param>
        private Parameters(
            IReadOnlyDictionary<string, ICollection<string>> defaults,
            IDictionary<string, ICollection<string>> args)
        {
            IDictionary<string, ICollection<string>> empty = null;
            if (defaults == null || args == null)
            {
                empty = new Dictionary<string, ICollection<string>>();
            }

            this.Defaults = defaults;
            this.Args = new ReadOnlyDictionary<string, ICollection<string>>(args ?? empty);
        }

        /// <summary>
        ///      Gets the result with the specified name
        /// </summary>
        /// <param name="name">index name</param>
        /// <returns>resulting value</returns>
        public ICollection<string> this[string name]
        {
            get
            {
                ICollection<string> result;

                if (this.Args.TryGetValue(name, out result) == false || result == null || result.Count == 0)
                {
                    if (this.Defaults.TryGetValue(name, out result) == false || result == null || result.Count == 0)
                    {
                        throw new ConfigException(name);
                    }
                }

                return result;
            }
        }

        /// <summary>
        ///      Parses the specified runner
        /// </summary>
        /// <param name="runner">runner</param>
        /// <param name="args">arguments</param>
        /// <returns>resulting value</returns>
        public static Parameters Parse(
            ICommandRunner runner,
            IList<string> args)
        {
            IDictionary<string, ICollection<string>> result = 
                new Dictionary<string, ICollection<string>>(StringComparer.InvariantCultureIgnoreCase);

            for (int idxArg = 0; idxArg < args.Count; ++idxArg)
            {
                ICollection<string> values;
                string arg = args[idxArg];
                char first;

                if (arg.Length == 0)
                {
                    throw new ArgumentException($"parameter name at position {idxArg} is empty");
                }

                first = arg[0];
                if (first != '-' && first != '/')
                {
                    throw new ArgumentException($"parameter name {arg} at position {idxArg} is does not start with '-' or '/'");
                }

                arg = runner.TranslateParamName(arg.Substring(1));

                if (runner.IsParamSwitch(arg))
                {
                    result[arg] = null;
                    continue;
                }

                if (result.TryGetValue(arg, out values) == false)
                {
                    result[arg] = values = new List<string>();
                }

                if (idxArg == args.Count - 1)
                {
                    throw new ArgumentException($"parameter name {arg} at position {idxArg} has no values");
                }

                values.Add(args[++idxArg]);
            }

            return new Parameters(runner.Defaults, result);
        }
    }
}
