// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Helpers
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Service Helper
    /// </summary>
    public static class ServiceHelper
    {
        public static async Task DeleteAndAssertAsync(
            ServiceResponse<DeleteResponseV1> expectedResponse,
            Func<IRequestContext, DeleteRequestV1, Task<ServiceResponse<DeleteResponseV1>>> deleteMethod)
        {
            IRequestContext requestContext = RequestContext.CreateOldStyle(new Uri("https://unittest"), string.Empty, null, default(long), default(long), null, default(long), null, null, 167, new string[0]);
            DeleteRequestV1 deleteRequest = new DeleteRequestV1 { DeleteAll = true };

            // Act
            ServiceResponse<DeleteResponseV1> actualResponse = await deleteMethod(requestContext, deleteRequest);

            // Assert
            Assert.IsNotNull(actualResponse);
            Assert.IsNotNull(actualResponse.Result);
            Assert.IsTrue(actualResponse.IsSuccess);
            Assert.AreEqual(expectedResponse.Result.Status, actualResponse.Result.Status);
        }

        public static async Task DeleteAndAssertAsync(
            ServiceResponse<DeleteResponseV1> expectedResponse,
            Func<IRequestContext, DeleteRequestV1, bool, Task<ServiceResponse<DeleteResponseV1>>> deleteMethod,
            IRequestContext requestContext,
            bool disableThrottling = false)
        {
            DeleteRequestV1 deleteRequest = new DeleteRequestV1 { DeleteAll = true };

            // Act
            ServiceResponse<DeleteResponseV1> actualResponse = await deleteMethod(requestContext, deleteRequest, disableThrottling);

            // Assert
            Assert.IsNotNull(actualResponse);
            Assert.IsNotNull(actualResponse.Result);
            Assert.IsTrue(actualResponse.IsSuccess);
            Assert.AreEqual(expectedResponse.Result.Status, actualResponse.Result.Status);
        }

        public static async Task DeleteAndAssertErrorAsync(
            ServiceResponse<DeleteResponseV1> expectedResponse,
            Func<IRequestContext, DeleteRequestV1, Task<ServiceResponse<DeleteResponseV1>>> deleteMethod)
        {
            IRequestContext requestContext = RequestContext.CreateOldStyle(new Uri("https://unittest"), string.Empty, null, default(long), default(long), null, default(long), null, null, 992, new string[0]);
            DeleteRequestV1 deleteRequest = new DeleteRequestV1 { DeleteAll = true };

            // Act
            ServiceResponse<DeleteResponseV1> actualResponse = await deleteMethod(requestContext, deleteRequest);

            // Assert
            Assert.IsNotNull(actualResponse);
            Assert.IsNull(actualResponse.Result);
            Assert.IsFalse(actualResponse.IsSuccess);
            EqualityHelper.AreEqual(expectedResponse.Error, actualResponse.Error);
        }

        public static async Task DeleteAndAssertErrorAsync(
            ServiceResponse<DeleteResponseV1> expectedResponse,
            Func<IRequestContext, DeleteRequestV1, bool, Task<ServiceResponse<DeleteResponseV1>>> deleteMethod,
            IRequestContext requestContext,
            bool disableThrottling = false)
        {
            DeleteRequestV1 deleteRequest = new DeleteRequestV1 { DeleteAll = true };

            // Act
            ServiceResponse<DeleteResponseV1> actualResponse = await deleteMethod(requestContext, deleteRequest, disableThrottling);

            // Assert
            Assert.IsNotNull(actualResponse);
            Assert.IsNull(actualResponse.Result);
            Assert.IsFalse(actualResponse.IsSuccess);
            EqualityHelper.AreEqual(expectedResponse.Error, actualResponse.Error);
        }   
    }
}