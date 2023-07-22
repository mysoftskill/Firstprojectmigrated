namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi
{
    using Ms.Qos;

    /// <summary>
    /// Extension functions for the IncomingServiceRequest class.
    /// </summary>
    public static class IncomingServiceRequestExtensions
    {
        /// <summary>
        /// Fills in the base data with the given operation metadata.
        /// </summary>
        /// <param name="baseData">The base data.</param>
        /// <param name="operationMetadata">The operation metadata.</param>
        public static void SetOperationMetadata(this IncomingServiceRequest baseData, OperationMetadata operationMetadata)
        {
            baseData.callerIpAddress = operationMetadata.CallerIpAddress;
            baseData.protocol = operationMetadata.Protocol;
            baseData.serviceErrorCode = operationMetadata.ProtocolStatusCode;
            baseData.requestMethod = operationMetadata.RequestMethod;
            baseData.requestSizeBytes = operationMetadata.RequestSizeBytes;
            baseData.responseContentType = operationMetadata.ResponseContentType;
            baseData.targetUri = operationMetadata.TargetUri;

            // Setting the Dictionary<string,string> to null would cause nullref in json serialization, so it should never be set to null. 
            // By default, generated class sets it to an empty dictionary.
            if (operationMetadata.CC != null)
            {
                baseData.cC = operationMetadata.CC;
            }
        }
    }
}
