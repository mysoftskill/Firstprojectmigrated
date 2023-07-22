namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// A simple interface to abstract the actual IO operation.
    /// </summary>
    public interface IConsoleWriter
    {
        /// <summary>
        /// Writes the given data to the console using the provided color.
        /// </summary>
        /// <param name="data">The data to write.</param>
        /// <param name="color">The color in which to write.</param>
        void WriteLine(string data, ConsoleColor color);
    }

    /// <summary>
    /// A concrete implementation that is used by production code.
    /// This is excluded from code coverage since it performs IO.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ConsoleWriter : IConsoleWriter
    {
        /// <summary>
        /// A singleton instance of the ConsoleWriter class.
        /// </summary>
        private static ConsoleWriter instance = new ConsoleWriter();

        /// <summary>
        /// Gets a singleton instance of the ConsoleWriter class.
        /// </summary>
        internal static ConsoleWriter Instance
        {
            get
            {
                return ConsoleWriter.instance;
            }
        }

        /// <summary>
        /// Writes the given data to the console using the provided color.
        /// </summary>
        /// <param name="data">The data to write.</param>
        /// <param name="color">The color in which to write.</param>
        public void WriteLine(string data, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(data);
        }
    }
}
