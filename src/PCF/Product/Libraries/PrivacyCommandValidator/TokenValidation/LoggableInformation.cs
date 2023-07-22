namespace Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation
{
    using System.Collections.Generic;

    /// <summary>
    /// The values to be passed around for included in exceptions that identify this validation request uniquely
    /// </summary>
    public struct LoggableInformation
    {
        /// <summary>
        /// Initilizes <see cref="LoggableInformation"/>
        /// </summary>
        /// <param name="commandId">commandId</param>
        /// <param name="jwtId">JwtId</param>
        /// <param name="commandSubject">CommandSubject</param>
        public LoggableInformation(string commandId, string commandSubject, string jwtId = null)
        {
            this.CommandId = commandId;
            this.JwtId = jwtId;
            this.CommandSubject = commandSubject;
        }

        /// <summary>
        /// CommandId of the command
        /// </summary>
        public string CommandId { get; }

        /// <summary>
        /// JWT Id of the verifier
        /// </summary>
        public string JwtId { get; set; }

        /// <summary>
        /// Subject of the command
        /// </summary>
        public string CommandSubject { get; }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (!(obj is LoggableInformation))
            {
                return false;
            }

            var information = (LoggableInformation)obj;
            return this.Equals(information);
        }

        /// <inheritdoc/>
        public bool Equals(LoggableInformation other)
        {
            return this.CommandId == other.CommandId &&
                   this.JwtId == other.JwtId &&
                   this.CommandSubject == other.CommandSubject;
        }

        /// <inheritdoc/>
        public static bool operator ==(LoggableInformation loggableInformation1, LoggableInformation loggableInformation2)
        {
            return loggableInformation1.Equals(loggableInformation2);
        }

        /// <inheritdoc/>
        public static bool operator !=(LoggableInformation loggableInformation1, LoggableInformation loggableInformation2)
        {
            return !loggableInformation1.Equals(loggableInformation2);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"CommandId : {this.CommandId}; JwtId: {this.JwtId}; CommandSubject: {this.CommandSubject}";
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCode = -560704998;
            hashCode = (hashCode * -1521134295) + base.GetHashCode();
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(this.CommandId);
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(this.CommandSubject);
            return hashCode;
        }
    }
}
