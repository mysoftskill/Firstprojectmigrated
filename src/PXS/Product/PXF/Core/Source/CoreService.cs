// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;

    using OrderByType = Microsoft.Membership.MemberServices.PrivacyAdapters.OrderByType;

    /// <summary>
    ///     CoreService
    /// </summary>
    public abstract class CoreService
    {
        private const int MaxPageReads = 10;

        protected IPxfDispatcher PxfDispatcher { get; private set; }

        protected CoreService(IPxfDispatcher pxfDispatcher)
        {
            if (pxfDispatcher == null)
            {
                throw new ArgumentNullException(nameof(pxfDispatcher));
            }

            this.PxfDispatcher = pxfDispatcher;
        }

        protected static Error CheckDispatcherResponseForError(DeletionResponse<DeleteResourceResponse> dispatcherResponse)
        {
            if (dispatcherResponse?.Items == null || !dispatcherResponse.Items.Any())
            {
                return new Error(ErrorCode.PartnerError, ErrorMessages.PartnerNullResponse);
            }

            if (dispatcherResponse.Items.All(c => c.Status == ResourceStatus.Deleted))
            {
                return null;
            }

            // If one of the adapters did not delete, this should be a QOS impacting error so we know about it.
            var errorMessage = new StringBuilder();
            errorMessage.Append("Failed requests to outbound partners. ");
            foreach (DeleteResourceResponse partnerErrorResponse in dispatcherResponse.Items.Where(c => c.Status != ResourceStatus.Deleted))
            {
                errorMessage.AppendLine();
                errorMessage.Append(
                    $"Partner id: '{partnerErrorResponse.PartnerId}', Status: '{partnerErrorResponse.Status}', Error Message: '{partnerErrorResponse.ErrorMessage}'. ");
            }

            return new Error(ErrorCode.PartnerError, errorMessage.ToString());
        }

        protected static void OrderResourcesV1<T>(OrderByType? orderByType, ref IList<T> items) where T : ResourceV1
        {
            if (items == null)
            {
                return;
            }

            switch (orderByType)
            {
                case OrderByType.DateTime:
                    items = items.OrderByDescending(i => i.DateTime).ToList();
                    break;

                case OrderByType.SearchTerms:

                // this concept does not apply to this resource
                default:

                    // do nothing;
                    break;
            }
        }
    }
}
