// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.Utilities
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class CommandPromptRequestException : Exception
    {
        public CommandPromptRequestException() : base()
        {
        }

        public CommandPromptRequestException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public CommandPromptRequestException(string message) : base(message)
        {
        }

        protected CommandPromptRequestException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public CommandPromptRequestException(string command, int exitCode, string output)
        {
            this.Output = output;
            this.Command = command;
            this.ExitCode = exitCode;
        }

        public string Command { get; private set; }

        public int ExitCode { get; private set; }

        public string Output { get; private set; }

        public override string Message
        {
            get
            {
                return "Command exited with a non-success exit code ({0}). Command: \"{1}\", Output \"{2}\"".FormatInvariant(this.ExitCode, this.Command, this.Output);
            }
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}
