//---------------------------------------------------------------------
// <copyright file="SourceLocation.cs" company="Microsoft">
//   Copyright (C) Microsoft. All rights reserved.
// </copyright>
//---------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler
{
    using System.IO;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Defines a location in source code.
    /// </summary>
    public class SourceLocation
    {
        /// <summary>
        /// Creates a new source location at the given position.
        /// </summary>
        /// <param name="memberName">Name of the calling function</param>
        /// <param name="filePath">Name of the calling file</param>
        /// <param name="lineNumber">Line number called from</param>
        public SourceLocation(string memberName, string filePath, int lineNumber)
        {
            this.FilePath = filePath;
            this.MemberName = memberName;
            this.LineNumber = lineNumber;
            this.FileName = Path.GetFileNameWithoutExtension(filePath);
        }

        /// <summary>
        /// Gets the current location.
        /// </summary>
        /// <param name="memberName">Name of the calling function</param>
        /// <param name="filePath">Name of the calling file</param>
        /// <param name="lineNumber">Line number called from</param>
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
        /// Gets the file name (without extension).
        /// </summary>
        public string FileName
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
    }
}
