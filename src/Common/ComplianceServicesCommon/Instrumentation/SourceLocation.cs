namespace Microsoft.Azure.ComplianceServices.Common.Instrumentation
{
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Defines a location in source code.
    /// </summary>
    public sealed class SourceLocation
    {
        /// <summary>
        /// A cache of File Path -> Name resolutions, since Extracting File Name from Path is (relatively) expensive.
        /// </summary>
        private static readonly ConcurrentDictionary<string, string> FilePathNameLookup = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Creates a new source location at the given position.
        /// </summary>
        [DebuggerStepThrough]
        public SourceLocation(string memberName, string filePath, int lineNumber)
        {
            this.FilePath = filePath;
            this.MemberName = memberName;
            this.LineNumber = lineNumber;
            
            if (!FilePathNameLookup.TryGetValue(this.FilePath, out string fileName))
            {
                fileName = Path.GetFileNameWithoutExtension(this.FilePath);
                FilePathNameLookup[this.FilePath] = fileName;
            }

            this.FileName = fileName;
        }

        /// <summary>
        /// Gets the current location.
        /// </summary>
        [DebuggerStepThrough]
        public static SourceLocation Here(
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = -1)
        {
            return new SourceLocation(memberName, filePath, lineNumber);
        }

        /// <summary>
        /// Gets the member name.
        /// </summary>
        public string MemberName
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the file path.
        /// </summary>
        public string FilePath
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the line number.
        /// </summary>
        public int LineNumber
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the file name (without extension).
        /// </summary>
        public string FileName
        {
            get;
            private set;
        }
    }
}