// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using System.Web.Services.Protocols;
    using System.Xml;
    using System.Xml.XPath;

    using Microsoft.Membership.MemberServices.Adapters.Common.DelegatingExecutors;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;

    /// <inheritdoc />
    public class LogicalOperationMsaIdentityServiceHandler : DelegatingWcfHandler
    {
        private readonly OutgoingApiEventWrapper apiEvent;

        /// <inheritdoc />
        public LogicalOperationMsaIdentityServiceHandler(OutgoingApiEventWrapper apiEvent)
        {
            this.apiEvent = apiEvent;
        }

        /// <summary>
        ///     Execute and track a WCF operation. Unhandled exceptions and non-success responses are considered failures.
        ///     Additionally, this custom handler catches <see cref="SoapException" /> specific to MsaIdentityService
        /// </summary>
        /// <typeparam name="T">Specifies the contract type the WCF operation returns.</typeparam>
        /// <param name="action">A function which executes a WCF operation.</param>
        /// <returns>An asynchronous task executing the delegate.</returns>
        public override async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
        {
            // Pre-Execution
            this.apiEvent.Start();

            // Execute
            T response;
            try
            {
                response = await base.ExecuteAsync(action);
            }
            catch (SoapException soapException)
            {
                this.apiEvent.Success = false;

                if (this.TryParse(soapException, out MsaIdentityServiceException idsapiException))
                {
                    this.apiEvent.ErrorMessage = idsapiException.Description;
                    this.apiEvent.ExtraData?.Add(nameof(idsapiException.ErrorCode), $"0x{idsapiException.ErrorCode.ToString("X")}");
                    this.apiEvent.ExtraData?.Add(nameof(idsapiException.InternalErrorText), idsapiException.InternalErrorText);
                    this.apiEvent.ExtraData?.Add(nameof(idsapiException.InternalErrorCode), $"0x{idsapiException.InternalErrorCode.ToString("X")}");

                    switch (idsapiException.ErrorCode)
                    {
                        // Auth denied shouldn't be QOS impacting
                        case 0x80048101:
                        case 0x80048105:
                        case 0x80045024:
                            this.apiEvent.ProtocolStatusCode = ((int)HttpStatusCode.Unauthorized).ToString();
                            break;

                        // For everything else, treat it as a 500 since we don't know what it is.
                        // If this happens, either it's legitimately a server error, or it should prompt us to find out what the error actually is
                        // Error codes (hex values) can be searched for @ https://errors
                        default:
                            this.apiEvent.ProtocolStatusCode = "500";
                            break;
                    }

                    throw idsapiException;
                }
                else
                {
                    // Soap exception does not expose the details in a stack trace. But 'Detail' element does
                    string soapErrorDetails = ((XmlElement)soapException.Detail)?.InnerXml;
                    this.apiEvent.ErrorMessage = soapErrorDetails;
                }

                throw;
            }
            catch (Exception ex)
            {
                this.apiEvent.Success = false;
                this.apiEvent.ErrorMessage = ex.Message;
                this.apiEvent.ServiceErrorCode = 500;

                throw;
            }
            finally
            {
                // Post-Execution
                this.apiEvent.Finish();
            }

            return response;
        }

        internal bool TryParse(SoapException soapException, out MsaIdentityServiceException idsapiException)
        {
            // NOTE: re-using exception parsing from another MEE team @
            // https://microsoft.visualstudio.com/Universal%20Store/_git/MEE.Devices.Directory.Svc?path=%2FProduct%2FEntityProviders%2FMsaProxyProvider%2FSource%2FMsaSoapClient.cs&version=GBmaster

            idsapiException = new MsaIdentityServiceException(
                soapException.Message,
                soapException.Code,
                soapException.Actor,
                soapException.Role,
                soapException.Detail,
                soapException.SubCode,
                soapException);

            try
            {
                XmlNode node = idsapiException.Detail;

                if (node != null)
                {
                    XPathNavigator navigator = node.CreateNavigator();
                    idsapiException.Description = navigator.SelectSingleNode("/psf:error/psf:description/psf:text", NamespaceManager)?.Value;
                    string codeValue = navigator.SelectSingleNode("/psf:error/psf:value", NamespaceManager)?.Value;
                    idsapiException.ErrorCode = Convert.ToInt64(codeValue, 16);
                    idsapiException.InternalErrorText = navigator.SelectSingleNode("/psf:error/psf:internalerror/psf:text", NamespaceManager)?.Value;

                    string internalErrorCodeValue = navigator.SelectSingleNode("/psf:error/psf:internalerror/psf:code", NamespaceManager)?.Value;
                    idsapiException.InternalErrorCode = !string.IsNullOrEmpty(internalErrorCodeValue) ? Convert.ToInt64(internalErrorCodeValue, 16) : default(long);
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        private static XmlNamespaceManager NamespaceManager
        {
            get
            {
                var namespaceManager = new XmlNamespaceManager(new NameTable());
                namespaceManager.AddNamespace("psf", "http://schemas.microsoft.com/Passport/SoapServices/SOAPFault");
                return namespaceManager;
            }
        }
    }
}
