// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyHost
{
    using System;
    using System.Diagnostics;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Family.Client.JsonWebToken;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Enables late binding to config values
    /// </summary>
    public interface IFamilyDecoratorConfig
    {
        string FamilyJwks { get; }
    }

    public class FamilyDecorator : HostDecorator
    {
        private readonly IFamilyDecoratorConfig config;

        private readonly ILogger logger;

        public FamilyDecorator(IFamilyDecoratorConfig config, ILogger genevaLogger)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.logger = genevaLogger;
        }

        public override ConsoleSpecialKey? Execute()
        {
            Trace.TraceInformation("FamilyDecorator executing");

            using (var claimAuthenticator = new DefaultClaimAuthenticator())
            {
                Console.WriteLine($"Running InitializeAsync");
                claimAuthenticator.InitializeAsync(
                    new Uri(this.config.FamilyJwks),
                    FamilyClaims.FamilyIssuer,
                    "ngpcieng@microsoft.com",
                    TimeSpan.FromDays(1),
                    ex =>
                    {
                        new ErrorEvent
                        {
                            ComponentName = nameof(FamilyDecorator),
                            ErrorMethod = nameof(this.Execute),
                            ErrorMessage = ex.Message,
                            ErrorType = ex.GetType().FullName,
                            ErrorCode = ex.HResult.ToString(),
                            CallStack = ex.StackTrace
                        }.LogError();
                    }).GetAwaiter().GetResult();

                FamilyClaims.Initialize(claimAuthenticator);

                return base.Execute();
            }
        }
    }
}