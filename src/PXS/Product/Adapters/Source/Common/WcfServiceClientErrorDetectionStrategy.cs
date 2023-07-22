// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Adapters.Common
{
    using System;
    using System.ServiceModel;
    using System.ServiceModel.Security;
    using Microsoft.Practices.TransientFaultHandling;

    /// <summary>
    /// Custom transient error detection strategy for the WCF Service client. 
    /// </summary>
    public class WcfServiceClientDetectionStrategy : ITransientErrorDetectionStrategy
    {
        private static readonly Lazy<WcfServiceClientDetectionStrategy> instance =
            new Lazy<WcfServiceClientDetectionStrategy>(() => new WcfServiceClientDetectionStrategy());

        public static WcfServiceClientDetectionStrategy Instance
        {
            get
            {
                return instance.Value;
            }
        }

        public bool IsTransient(Exception ex)
        {
            // If there was an application fault
            if (ex is FaultException)
            {
                return false;
            }

            // Other non-retryable types derived from CommunicationException
            if (ex is ActionNotSupportedException || ex is AddressAccessDeniedException || ex is PoisonMessageException ||
                ex is ProtocolException || ex is MessageSecurityException || ex is SecurityAccessDeniedException ||
                ex is SecurityNegotiationException)
            {
                return false;
            }

            // For any other CommunicationException (including base type)
            if (ex is CommunicationException)
            {
                return true;
            }

            // If there was a client timeout
            if (ex is TimeoutException)
            {
                return true;
            }

            PartnerException partnerException = ex as PartnerException;
            if (partnerException != null)
            {
                return partnerException.IsRetryable;
            }

            // For any other unexpected exception type (unlikely)
            return false;
        }
    }
}
