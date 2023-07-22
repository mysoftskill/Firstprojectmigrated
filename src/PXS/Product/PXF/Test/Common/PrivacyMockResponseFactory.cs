// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;

    /// <summary>
    /// Privacy-Mock-Response Factory
    /// </summary>
    public static class PrivacyMockResponseFactory
    {
        /// <summary>
        /// Creates the aggregated response for a <see cref="Resource"/>.
        /// </summary>
        /// <returns>A new instance of <see cref="AggregatedResponse{T}"/></returns>
        public static AggregatedResponse<T> CreateAggregatedResponse<T>(int numberResults = 1) where T: Resource, new()
        {
            List<T> items = new List<T>();

            for (int i = 0; i < numberResults; i++)
            {
                items.Add(new T { Id = $"test_id{i}", DateTime = DateTimeOffset.Parse("2016-01-15")});
            }

            AggregatedResponse<T> mockResponse = new AggregatedResponse<T>
            {
                Items = items
            };

            return mockResponse;
        }
    }
}