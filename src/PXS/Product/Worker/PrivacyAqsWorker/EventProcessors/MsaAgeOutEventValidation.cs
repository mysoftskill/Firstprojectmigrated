// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.EventProcessors
{
    using System;
    using System.Collections.Generic;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging.Extensions;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common;
    using Microsoft.Membership.MemberServices.Privacy.Core.AQS;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    public static class MsaAgeOutEventValidation
    {
        private static LogOption DefaultLogOption => LogOption.Realtime;

        private static MsaAgeOutEvent CreateMsaAgeOutEvent(AccountDeleteInformation adi)
        {
            return new MsaAgeOutEvent
            {
                RequestGuid = adi.RequestGuid.ToString(),
                RequestId = adi.CommandId.ToString(),
                RequestTimestamp = adi.TimeStamp.ToString("u"),
                ErrorCodes = new Dictionary<MsaAgeOutErrorCode, string>(),
                LastSuccessSignIn = string.Empty
            };
        }

        private static void IncrementMsaAgeOutError(ICounterFactory counterFactory, MsaAgeOutErrorCode errorCode)
        {
            ICounter counter = counterFactory.GetCounter(CounterCategoryNames.MsaAgeOut, "failure", CounterType.Rate);
            counter.Increment();
            counter.Increment(errorCode.ToString());
        }

        private static void IncrementMsaAgeOutSuccess(ICounterFactory counterFactory)
        {
            ICounter counter = counterFactory.GetCounter(CounterCategoryNames.MsaAgeOut, "success", CounterType.Rate);
            counter.Increment();
        }

        internal static void ProcessMsaAgeOutEvent(
            AccountDeleteInformation adi,
            CDPEvent2 evt,
            CdpEvent2Helper eventHelper,
            IClock clock,
            ILogger logger,
            ICounterFactory counterFactory)
        {
            if (adi.Reason == AccountCloseReason.UserAccountAgedOut)
            {
                DateTimeOffset now = clock.UtcNow;
                MsaAgeOutEvent msaAgeOutEvent = CreateMsaAgeOutEvent(adi);
                UserInfo userInfoAction = SllLoggingHelper.CreateUserInfo(UserIdType.DecimalPuid, adi.Puid.ToString());
                if (eventHelper.TryGetLastLogin(evt, out DateTimeOffset lastLogin))
                {
                    adi.LastLogin = lastLogin;
                    msaAgeOutEvent.LastSuccessSignIn = lastLogin.ToString("u");

                    // the last login time is used for filtering these commands to agents downstream, so if for some reason we have some invalid time, we want to know about it.
                    TimeSpan age = now - lastLogin;
                    msaAgeOutEvent.SignalAge = age.ToString();

                    if (lastLogin > now)
                    {
                        string description = $"ErrorCode: {MsaAgeOutErrorCode.LastLoginTimeInFuture} " +
                                             "Contact the MSA team and inform them the 'LastSuccessSignIn' is in the future for this puid. " +
                                             $"LastSuccessSignIn: '{lastLogin:u}'. Current time: '{now:u}'. Age of last activity: '{age}'";
                        msaAgeOutEvent.ErrorCodes.Add(MsaAgeOutErrorCode.LastLoginTimeInFuture, description);
                        logger.Error(nameof(MsaAgeOutEventValidation), description);
                    }
                    else if (lastLogin > now.AddYears(-2))
                    {
                        string description = $"ErrorCode: {MsaAgeOutErrorCode.LastLoginLessThan2Years} " +
                                             "Contact the MSA team and inform them the 'LastSuccessSignIn' is within the last 2 years. " +
                                             "This is a violation of Microsoft Services Agreement policy for this user puid. " +
                                             $"LastSuccessSignIn: '{lastLogin:u}'. Current time: '{now:u}'. Age of last activity: '{age}'";
                        msaAgeOutEvent.ErrorCodes.Add(MsaAgeOutErrorCode.LastLoginLessThan2Years, description);
                        logger.Error(nameof(MsaAgeOutEventValidation), description);
                    }
                    else if (lastLogin > now.AddYears(-5))
                    {
                        string description = $"ErrorCode: {MsaAgeOutErrorCode.LastLoginLessThan5Years} " +
                                             "Contact the MSA team and inform them the 'LastSuccessSignIn' is within the last 5 years. " +
                                             "This is a violation of Microsoft Services Agreement policy for this user puid. " +
                                             "Note: this policy is expected to change, and we should expect to see values within this time in the year 2021. At that point, this error condition can be removed. " +
                                             $"LastSuccessSignIn: '{lastLogin:u}'. Current time: '{now:u}'. Age of last activity: '{age}'";
                        msaAgeOutEvent.ErrorCodes.Add(MsaAgeOutErrorCode.LastLoginLessThan5Years, description);
                        logger.Error(nameof(MsaAgeOutEventValidation), description);
                    }
                }
                else
                {
                    string description = $"ErrorCode: {MsaAgeOutErrorCode.MissingLastLoginTime} " +
                                         "Contact the MSA team and inform them the 'LastSuccessSignIn' is missing for this user puid. This should never happen.";
                    msaAgeOutEvent.ErrorCodes.Add(MsaAgeOutErrorCode.MissingLastLoginTime, description);
                    logger.Error(nameof(MsaAgeOutEventValidation), description);
                }

                if (eventHelper.TryGetIsSuspended(evt, out bool isSuspended))
                {
                    adi.IsSuspended = isSuspended;
                }
                else
                {
                    string description = $"ErrorCode: {MsaAgeOutErrorCode.MissingIsSuspendedValue} " +
                                         $"Contact the MSA team and inform them the '{nameof(adi.IsSuspended)}' value is missing for this user puid. This should never happen.";
                    msaAgeOutEvent.ErrorCodes.Add(MsaAgeOutErrorCode.MissingIsSuspendedValue, description);
                    logger.Error(nameof(MsaAgeOutEventValidation), description);
                }

                if (msaAgeOutEvent.ErrorCodes.Count == 0)
                {
                    msaAgeOutEvent.LogInformational(DefaultLogOption, userInfoAction.FillEnvelope);
                    IncrementMsaAgeOutSuccess(counterFactory);
                }
                else
                {
                    msaAgeOutEvent.LogError(userInfoAction.FillEnvelope);
                    foreach (MsaAgeOutErrorCode errorCode in msaAgeOutEvent.ErrorCodes?.Keys)
                    {
                        IncrementMsaAgeOutError(counterFactory, errorCode);
                    }
                }
            }
        }
    }
}
