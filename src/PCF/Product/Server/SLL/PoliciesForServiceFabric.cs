﻿// <auto-generated />
// <copyright>Copyright (c) Microsoft Corporation. All rights reserved.</copyright>

using Microsoft.CommonSchema.Services.Logging;
using Microsoft.CommonSchema.Services.ServiceFabricContainer;

namespace Microsoft.PrivacyServices.CommandFeed.Service.Instrumentation
{
    /// <summary>
    /// SLL policy container class. Do not edit.
    /// </summary>
    public partial class Policies : PolicyRegistration
    {
        /// <summary>
        /// Gets the policy configuration for a Service Fabric based service.
        /// </summary>
        public static ServiceFabricContainerPolicy ServiceFabric
        {
            get
            {
                return Single<ServiceFabricContainerPolicy>.Current;
            }
        }
    }
}
