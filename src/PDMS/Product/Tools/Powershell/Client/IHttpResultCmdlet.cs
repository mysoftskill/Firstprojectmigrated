namespace Microsoft.PrivacyServices.DataManagement.Client.Powershell
{
    /// <summary>
    /// A base class for handling IHttpResult without a return value.
    /// </summary>
    public abstract class IHttpResultCmdlet : BaseServiceCmdlet<IHttpResult>
    {
        /// <summary>
        /// Takes no action because there is no response.
        /// </summary>
        /// <param name="result">The result.</param>
        protected override void WriteResult(IHttpResult result)
        {
            // There is nothing to write for results with no response.
        }

        /// <summary>
        /// A base class for handling IHttpResults from ServiceTree client.
        /// </summary>
        public abstract class ServiceTree : BaseServiceTreeCmdlet<IHttpResult>
        {
            /// <summary>
            /// Takes no action because there is no response.
            /// </summary>
            /// <param name="result">The result.</param>
            protected override void WriteResult(IHttpResult result)
            {
                // There is nothing to write for results with no response.
            }
        }
    }
}