namespace Microsoft.PrivacyServices.DataManagement.Client.Powershell
{
    using System.Collections;

    /// <summary>
    /// A base class for handling IHttpResult with a return value.
    /// </summary>
    /// <typeparam name="T">The return type of the IHttpResult.</typeparam>
    public abstract class IHttpResultCmdlet<T> : BaseServiceCmdlet<IHttpResult<T>>
    {
        /// <summary>
        /// Writes the result to the PowerShell pipeline.
        /// If the result is a collection, then it writes each item
        /// to the PowerShell pipeline individually.
        /// </summary>
        /// <param name="result">The result.</param>
        protected override void WriteResult(IHttpResult<T> result)
        {
            if (result != null)
            {
                var values = result.Response as IEnumerable;

                if (values != null)
                {
                    foreach (var value in values)
                    {
                        this.WriteObject(value);
                    }
                }
                else
                {
                    this.WriteObject(result.Response);
                }
            }
        }

        /// <summary>
        /// A base class for handling IHttpResults from ServiceTree client.
        /// </summary>
        public abstract class ServiceTree : BaseServiceTreeCmdlet<IHttpResult<T>>
        {
            /// <summary>
            /// Writes the result to the PowerShell pipeline.
            /// If the result is a collection, then it writes each item
            /// to the PowerShell pipeline individually.
            /// </summary>
            /// <param name="result">The result.</param>
            protected override void WriteResult(IHttpResult<T> result)
            {
                if (result != null)
                {
                    var values = result.Response as IEnumerable;

                    if (values != null)
                    {
                        foreach (var value in values)
                        {
                            this.WriteObject(value);
                        }
                    }
                    else
                    {
                        this.WriteObject(result.Response);
                    }
                }
            }
        }
    }
}