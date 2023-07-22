// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.Logging.Logging
{
    using System;
    using System.Reflection;

    using Microsoft.CommonSchema.Services;
    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Telemetry;
    using Microsoft.Telemetry.Extensions;

    /// <summary>
    ///     set of constant properties used in logging
    /// </summary>
    public static class LoggingInfo
    {
        private const string UnknownLocation = "Unknown";

        public static readonly string CurrentAssemblyName = Assembly.GetExecutingAssembly().GetName().Name;

        /// <summary>
        ///     Gets the cloud environment.
        /// </summary>
        /// <remarks>
        ///     See Xpert Wiki for documentation suggesting to read this via environment variable:
        ///     https://osgwiki.com/wiki/Xpert/Onboarding/Service/AzurePaaS#Correcting_SLL_PartA_Data and
        ///     https://osgwiki.com/wiki/Part_A_Fields_Populated_by_SLL
        /// </remarks>
        public static string CloudEnvironment { get; } =
            LoggingInfo.LoadEnvironmentVariableOrDefault(CloudServiceEnvironmentVariableName.CloudEnvironmentPropName);

        /// <summary>
        ///     Gets the cloud location.
        /// </summary>
        /// <remarks>
        ///     See Xpert Wiki for documentation suggesting to read this via environment variable:
        ///     https://osgwiki.com/wiki/Xpert/Onboarding/Service/AzurePaaS#Correcting_SLL_PartA_Data and
        ///     https://osgwiki.com/wiki/Part_A_Fields_Populated_by_SLL
        /// </remarks>
        public static string CloudLocation { get; } =
            LoggingInfo.LoadEnvironmentVariableOrDefault(CloudServiceEnvironmentVariableName.CloudLocationPropName);

        /// <summary>
        ///     Gets the cloud name.
        /// </summary>
        /// <remarks>
        ///     See Xpert Wiki for documentation suggesting to read this via environment variable:
        ///     https://osgwiki.com/wiki/Xpert/Onboarding/Service/AzurePaaS#Correcting_SLL_PartA_Data and
        ///     https://osgwiki.com/wiki/Part_A_Fields_Populated_by_SLL
        /// </remarks>
        public static string CloudName { get; } =
            LoggingInfo.LoadEnvironmentVariableOrDefault(CloudServiceEnvironmentVariableName.CloudEnvironmentName);

        /// <summary>
        ///     Gets the cloud role.
        /// </summary>
        /// <remarks>
        ///     See Xpert Wiki for documentation suggesting to read this via environment variable:
        ///     https://osgwiki.com/wiki/Xpert/Onboarding/Service/AzurePaaS#Correcting_SLL_PartA_Data and
        ///     https://osgwiki.com/wiki/Part_A_Fields_Populated_by_SLL
        /// </remarks>
        public static string CloudRole { get; } =
            LoggingInfo.LoadEnvironmentVariableOrDefault(CloudServiceEnvironmentVariableName.CloudRolePropName);

        /// <summary>
        ///     Fills the event envelope with basic cloud information
        /// </summary>
        /// <param name="envelope">envelope</param>
        public static void FillEnvelope(Envelope envelope)
        {
            //Fill envelope based on Correlation context
            Sll.Context?.CorrelationContext?.FillEnvelope(envelope);

            cloud cloud = envelope.SafeCloud();
            cloud.environment = LoggingInfo.CloudEnvironment;
            cloud.role = LoggingInfo.CloudRole;

            // This is an odd difference in hosting environments for SLL. AP correctly sets this, but Azure Cloud Service sets it
            //  to the assembly name.  So do this check to override it when the name is the assembly name only.
            if (LoggingInfo.CurrentAssemblyName.EqualsIgnoreCase(cloud.name))
            {
                cloud.name = LoggingInfo.CloudName;
            }

            // Value differs in AP vs Cloud Service, so only set when it's unknown.
            if (LoggingInfo.UnknownLocation.EqualsIgnoreCase(cloud.location))
            {
                cloud.location = LoggingInfo.CloudLocation;
            }
        }

        /// <summary>
        ///     Loads the environment variable or default
        /// </summary>
        /// <param name="name">environment variable name</param>
        /// <returns>resulting value</returns>
        private static string LoadEnvironmentVariableOrDefault(string name)
        {
            string value = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrWhiteSpace(value) == false ? value : LoggingInfo.UnknownLocation;
        }
        
        /// <summary>
        ///     Cloud Service Environment Variable Names represents the variables that correspond to meaningful values
        ///      when hosted in Azure Cloud Service. The associated keys and values can be seen in the
        ///      Azure Portal > Cloud Services > (Select Environment) > Settings > Configuration
        ///     Then, check the *.csdef to associate the setting key name with the environment variable name found in the *.csdef
        /// </summary>
        /// <example>
        ///     From the ServiceDefinition.BFPROD.csdef (found in build output), observe the setting visible in Azure Portal
        ///       'Environment' maps to the env variable 'MONITORING_ENVIRONMENT'
        ///     <![CDATA[ 
        ///     <Variable name="MONITORING_ENVIRONMENT">
        ///         <RoleInstanceValue xpath =
        ///            "/RoleEnvironment/CurrentInstance/ConfigurationSettings/ConfigurationSetting[@name='Environment']/@value" />
        ///     </Variable > 
        ///     ]]>
        /// </example>
        private static class CloudServiceEnvironmentVariableName
        {
            /// <summary>
            ///     Cloud Environment (type)
            /// </summary>
            /// <example>Production, Test</example>
            public const string CloudEnvironmentPropName = "MONITORING_ENVIRONMENT";

            /// <summary>
            ///     Cloud Environment Name
            /// </summary>
            /// <example>MEE-NGPProxy-NonPROD-EUS</example>
            public const string CloudEnvironmentName = "XpertEnvironmentName";

            /// <summary>
            ///     Cloud Location, aka Region
            /// </summary>
            /// <example>EUS</example>
            public const string CloudLocationPropName = "MONITORING_DATACENTER";

            /// <summary>
            ///     Cloud Role
            /// </summary>
            /// <example>NGPProxy.Service</example>
            public const string CloudRolePropName = "XpertRoleName";
        }
    }
}
