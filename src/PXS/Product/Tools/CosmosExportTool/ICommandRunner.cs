// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace CosmosExportScanner
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    ///     ICommandRunner interface
    /// </summary>
    public interface ICommandRunner
    {
        /// <summary>
        ///      Gets defaults
        /// </summary>
        IReadOnlyDictionary<string, ICollection<string>> Defaults { get; }

        /// <summary>
        ///      Translates the parameter name
        /// </summary>
        /// <param name="input">param name</param>
        /// <returns>translated name</returns>
        string TranslateParamName(string input);

        /// <summary>
        ///      determines whether the specified input is a parameter switch
        /// </summary>
        /// <param name="input">param name</param>
        /// <returns>true if the input is a switch, false otherwise</returns>
        bool IsParamSwitch(string input);

        /// <summary>
        ///      Runs the asynchronous
        /// </summary>
        /// <param name="args">task arguments</param>
        /// <returns>result as a collection of strings- one string per result row</returns>
        Task<ICollection<string>> RunAsync(Parameters args);

        /// <summary>
        ///      called when the task is complete
        /// </summary>
        /// <returns>resulting value</returns>
        Task OnComplete();
    }
}
