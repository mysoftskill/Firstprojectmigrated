// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace CosmosExportScanner
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    ///      CommandRunner class
    /// </summary>
    public abstract class CommandRunner : ICommandRunner
    {
        /// <summary>
        ///      Gets defaults
        /// </summary>
        public abstract IReadOnlyDictionary<string, ICollection<string>> Defaults { get; }

        /// <summary>
        ///      Translates the parameter name
        /// </summary>
        /// <param name="input">param name</param>
        /// <returns>resulting value</returns>
        public string TranslateParamName(string input) => input?.ToLowerInvariant();

        /// <summary>
        ///      determines whether [is parameter switch] [the specified input]
        /// </summary>
        /// <param name="input">param name</param>
        /// <returns>resulting value</returns>
        public bool IsParamSwitch(string input) => false;

        /// <summary>
        ///      Runs the asynchronous
        /// </summary>
        /// <param name="args">task arguments</param>
        /// <returns>resulting value</returns>
        public abstract Task<ICollection<string>> RunAsync(Parameters args);

        /// <summary>
        ///      called when the task is complete
        /// </summary>
        /// <returns>resulting value</returns>
        public virtual Task OnComplete() => Task.CompletedTask;
    }
}
