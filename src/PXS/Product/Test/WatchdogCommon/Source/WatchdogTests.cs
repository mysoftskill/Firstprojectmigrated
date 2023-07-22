// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.WatchdogCommon
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Contracts.Exposed;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.Oss.Membership.CommonCore.Extensions;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Helper class for writing watchdog tests.
    ///     - Logs perf counters for server calls.
    ///     - Allows for tests to have a minimum version. If the version returned by the server is less than the minimum
    ///     version, then the test result status will be unknown.
    /// </summary>
    public class WatchdogTests
    {
        private readonly string componentName;

        protected readonly ICounterFactory counterFactory;

        protected readonly ILogger logger;

        public static bool ValidateSuccessOrConflictOrTooManyRequestsStatusCodes(HttpResponseMessage response)
        {
            return response.IsSuccessStatusCode
                   || response.StatusCode == HttpStatusCode.Conflict
                   || (int)response.StatusCode == 429;
        }

        public static bool ValidateSuccessStatusCode(HttpResponseMessage response)
        {
            return response.IsSuccessStatusCode;
        }

        public WatchdogTests(string componentName, ILogger logger, ICounterFactory counterFactory)
        {
            this.componentName = componentName;
            this.logger = logger;
            this.counterFactory = counterFactory;
        }

        /// <summary>
        ///     Executes a test. Logs perf counters for the callServer() implementation.
        /// </summary>
        /// <param name="testName">The name of the test being executed. This is also the perf counter instance name.</param>
        /// <param name="minServerVersion">
        ///     The minimum service version that this test should execute upon. If the
        ///     server version returned by the service ("server-verion" header) is less than this version, then
        ///     validateResponse will not be executed, and the test status will be Unknown
        /// </param>
        /// <param name="callServer">Function that calls the server.</param>
        /// <param name="validateResponse">Function that validates the server response message.</param>
        /// <param name="getTestResultForValidResponse">(Optional) Function that provides the test result if generic test result is not acceptable</param>
        /// <returns>Test tesult</returns>
        protected async Task<TestResult> ExecuteTest(
            string testName,
            Version minServerVersion,
            Func<Task<HttpResponseMessage>> callServer,
            Func<HttpResponseMessage, bool> validateResponse,
            Func<Task<TestResult>> getTestResultForValidResponse = null)
        {
            this.logger.Information(this.componentName, "Beginning test {0}", testName);

            TimedHttpOperationExecutionResult httpExecutionResult =
                await RequestExecutionHelper.ExecuteTimedHttpActionAsync(
                    this.counterFactory,
                    CounterCategoryNames.PrivacyExperienceServiceWatchdog,
                    testName,
                    callServer);

            ulong latency = httpExecutionResult.LatencyInMilliseconds;

            TestStatus status;
            string message;
            Version serverVersion = null;
            if (httpExecutionResult.Exception == null)
            {
                try
                {
                    // The call did not result in an exception

                    // Get response as string for logging purposes
                    // Get server version for watchdog versioning comparison
                    string responseContent = null;
                    HttpResponseMessage response = httpExecutionResult.Response;
                    if (response != null)
                    {
                        serverVersion = this.GetServerVersion(response);
                        if (response.Content != null)
                        {
                            responseContent = await response.Content.ReadAsStringAsync();
                        }
                    }

                    // If the server is the minimum version, then continue executing validation. Otherwise, the test is
                    // marked as unconclusive (unknown status)
                    if (serverVersion != null && serverVersion >= minServerVersion)
                    {
                        // Determine test pass/failure and message from service response
                        if (validateResponse(response))
                        {
                            if (getTestResultForValidResponse == null)
                            {
                                status = TestStatus.Pass;
                                message = string.Format(
                                    CultureInfo.InvariantCulture,
                                    "Test {0} passed. Latency: {1}ms, Response: {2}",
                                    testName,
                                    latency,
                                    response);
                            }
                            else
                            {
                                var testResult = await getTestResultForValidResponse().ConfigureAwait(false);
                                status = testResult.Status;
                                message = testResult.Message;
                            }

                            var logMessage = string.Format(
                                CultureInfo.InvariantCulture,
                                "{0}, ResponseContent: {1}",
                                message,
                                responseContent);
                            this.logger.Information(this.componentName, logMessage);
                        }
                        else
                        {
                            status = TestStatus.Fail;
                            message = string.Format(
                                CultureInfo.InvariantCulture,
                                "Test {0} failed. Latency: {1}ms, Response: {2}",
                                testName,
                                latency,
                                response);
                            var logMessage = string.Format(
                                CultureInfo.InvariantCulture,
                                "{0}, ResponseContent: {1}",
                                message,
                                responseContent);
                            this.logger.Error(this.componentName, logMessage);
                        }
                    }
                    else
                    {
                        status = TestStatus.Unknown;
                        message = string.Format(
                            CultureInfo.InvariantCulture,
                            "Test {0} was inconclusive. MinServerVersion: {1}, ServerVersion: {2}, Latency: {3}ms, Response: {4}",
                            testName,
                            minServerVersion,
                            serverVersion,
                            latency,
                            response);
                        var logMessage = string.Format(
                            CultureInfo.InvariantCulture,
                            "{0}, ResponseContent: {1}",
                            message,
                            responseContent);
                        this.logger.Information(this.componentName, logMessage);
                    }
                }
                catch (Exception e)
                {
                    // prevent exceptions from killing the whole process, instead making it a test failure
                    status = TestStatus.Fail;
                    message = string.Format(
                        CultureInfo.InvariantCulture,
                        "Test {0} failed. Latency: {1}ms. Exception thrown: {2}",
                        testName,
                        latency,
                        e);
                    this.logger.Error(this.componentName, message);
                }
            }
            else
            {
                // The call threw an exception
                // Note that in this case we have no way to check the service version
                status = TestStatus.Fail;
                message = string.Format(
                    CultureInfo.InvariantCulture,
                    "Test {0} failed. Latency: {1}ms. Exception thrown: {2}",
                    testName,
                    latency,
                    httpExecutionResult.Exception);
                this.logger.Error(this.componentName, message);
            }

            this.logger.Information(this.componentName, "Finished test {0}", testName);

            return new TestResult
            {
                Name = testName,
                Status = status,
                Message = message
            };
        }

        private Version GetServerVersion(HttpResponseMessage response)
        {
            IEnumerable<string> serverVersionValues;
            if (response != null &&
                response.Headers != null &&
                response.Headers.TryGetValues(HeaderNames.ServerVersion, out serverVersionValues))
            {
                Version serverVersion;
                string serverVersionValue = response.Headers.GetValues(HeaderNames.ServerVersion).FirstOrDefault();
                if (!Version.TryParse(serverVersionValue, out serverVersion))
                {
                    this.logger.Error(
                        this.componentName,
                        "Unable to parse {0} header with value: {1}",
                        HeaderNames.ServerVersion,
                        serverVersionValue);
                }

                return serverVersion;
            }

            this.logger.Error(this.componentName, "{0} header was not found", HeaderNames.ServerVersion);
            return null;
        }

        protected static TestResult HandleAccessTokenRetrievalError(ErrorInfo error, string testName)
        {
            return new TestResult
            {
                Name = testName,
                Status = TestStatus.Unknown,
                Message = "Test {0} could not proceed because there was a failure retrieving the MSA access token: {1}".FormatInvariant(testName, error)
            };
        }

        protected static TestResult HandleUserProxyTicketRetrievalError(UserProxyTicketResult result, string testName)
        {
            return new TestResult
            {
                Name = testName,
                Status = TestStatus.Unknown,
                Message = "Test {0} could not proceed because there was a failure retrieving the user proxy ticket: {1}".FormatInvariant(testName, result.ErrorMessage)
            };
        }
    }
}
