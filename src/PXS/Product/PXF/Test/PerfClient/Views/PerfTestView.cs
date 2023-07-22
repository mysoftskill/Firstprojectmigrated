// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.PerfClient.Views
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Utilities;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.OSGS.HttpClientCommon;

    public class PerfTestView : IView
    {
        private readonly IRpsConfiguration rpsConfiguration;
        private readonly RequestType requestType;
        private TimeSpan sleepDuration;
        private readonly TimeSpan testDuration;
        private readonly IList<TestUser> testUsers;

        private RequestStatusModel StatusViewLocation = new RequestStatusModel("ViewLocation");
        private RequestStatusModel StatusViewHealthLocation = new RequestStatusModel("ViewHealthLocation");
        private RequestStatusModel StatusViewBrowse = new RequestStatusModel("ViewBrowse");
        private RequestStatusModel StatusViewSearch = new RequestStatusModel("ViewSearch");
        private RequestStatusModel StatusDeleteLocation = new RequestStatusModel("DeleteLocation");
        private RequestStatusModel StatusDeleteBrowse = new RequestStatusModel("DeleteBrowse");
        private RequestStatusModel StatusDeleteSearch = new RequestStatusModel("DeleteSearch");

        private int requestsPerSecond = 0;

        private IList<string> userProxyTicketsView = new List<string>();
        private IList<string> userProxyTicketsDelete = new List<string>();
        private IPrivacyAuthClient authClient;
        private IHttpClient httpClient;
        private List<Task> taskList = new List<Task>();

        private const int ViewPercentage = 95;
        private const int DeletePercentage = 5;

        private static Random random = new Random();
        private string logFileNameSuccess = $"{DateTime.Now.ToString("yyyy-MM-dd--hh-mm-ss")}_success.log";
        private string logFileNameError = $"{DateTime.Now.ToString("yyyy-MM-dd--hh-mm-ss")}_error.log";
        private string resultFileName = $"{DateTime.Now.ToString("yyyy-MM-dd--hh-mm-ss")}_results.log";
        private string logFileDirectory = "C:\\privacy\\perfLogs\\";
        private FileStream LogFileSuccess;
        private FileStream LogFileError;

        public bool ViewTestProbability
        {
            get { return random.Next(100) < ViewPercentage; }
        }

        public bool DeleteTestProbability
        {
            get { return random.Next(100) < DeletePercentage; }
        }

        public PerfTestView(
            RequestType requestType, 
            int requestsPerSecond, 
            TimeSpan testDuration,
            IList<TestUser> testUsers,
            IRpsConfiguration rpsConfiguration,
            IHttpClient httpClient,
            IPrivacyAuthClient authClient)
        {
            this.requestType = requestType;
            this.requestsPerSecond = requestsPerSecond;
            this.testDuration = testDuration;
            this.testUsers = testUsers;
            this.rpsConfiguration = rpsConfiguration;
            this.httpClient = httpClient;
            this.authClient = authClient;

            Directory.CreateDirectory(this.logFileDirectory);
            this.LogFileSuccess = new FileStream(
                    Path.Combine(this.logFileDirectory, this.logFileNameSuccess),
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.Read,
                    65536,
                    FileOptions.SequentialScan);
            this.LogFileError = new FileStream(
                Path.Combine(this.logFileDirectory, this.logFileNameError),
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read,
                65536,
                FileOptions.SequentialScan);
        }

        public void Render()
        {
            try
            {
                Console.WriteLine("Getting user proxy tickets for test users");
                foreach (var testUser in this.testUsers)
                {
                    switch (testUser.UserType)
                    {
                        case UserType.View:
                            this.userProxyTicketsView.Add(this.GetUserProxyTicket(testUser.UserName, testUser.Password));
                            break;
                        case UserType.Delete:
                            this.userProxyTicketsDelete.Add(this.GetUserProxyTicket(testUser.UserName, testUser.Password));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                Console.WriteLine($"Executing test type: {this.requestType} with target RPS: {this.requestsPerSecond}");
                Console.WriteLine($"Test Duration: {FormatTimeSpan(this.testDuration)}");
                this.sleepDuration = TimeSpan.FromMilliseconds(1000 / (double)this.requestsPerSecond);
                Console.Write($"Delay between requests: {this.sleepDuration}");

                Stopwatch timer = Stopwatch.StartNew();
                int loopIterations = 0;

                do
                {
                    switch (this.requestType)
                    {
                        case RequestType.View | RequestType.Delete:
                            this.RunViewTests();
                            this.RunDeleteTests();
                            break;
                        case RequestType.View:
                            this.RunViewTests();
                            break;
                        case RequestType.Delete:
                            this.RunDeleteTests();
                            break;
                        case RequestType.None:
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    if (loopIterations % 10 == 0)
                    {
                        this.RedrawScreen(this.CurrentTestStatus(timer));
                    }

                    loopIterations++;
                } while (timer.Elapsed < this.testDuration);

                timer.Stop();
                Task.WhenAll(this.taskList).Wait();
                this.taskList.Clear();
                var results = this.CurrentTestStatus(timer);
                this.RedrawScreen(results);
                Task.Delay(5000).Wait();

                File.WriteAllText(Path.Combine(this.logFileDirectory, this.resultFileName), results);
            }
            finally
            {
                this.LogFileSuccess?.Close();
                this.LogFileError?.Close();
            }
        }

        #region Delete

        private void RunDeleteTests()
        {
            if (this.DeleteTestProbability)
            {
                this.RunBrowseDeleteTest();
                Task.Delay(this.sleepDuration).Wait();

                this.RunSearchDeleteTest();
                Task.Delay(this.sleepDuration).Wait();

                this.RunLocationDeleteTest();
                Task.Delay(this.sleepDuration).Wait();

                // note: ms health doesn't delete data through our api
            }
        }

        private void RunLocationDeleteTest()
        {
            throw new NotImplementedException("Not supported for legacy V1 API's");
        }

        private void RunSearchDeleteTest()
        {
            throw new NotImplementedException("Not supported for legacy V1 API's");
        }

        private void RunBrowseDeleteTest()
        {
            throw new NotImplementedException("Not supported for legacy V1 API's");
        }

        #endregion

        #region View

        private void RunViewTests()
        {
            if (this.ViewTestProbability)
            {
                this.RunBrowseViewTest();
                Task.Delay(this.sleepDuration).Wait();

                this.RunSearchViewTest();
                Task.Delay(this.sleepDuration).Wait();

                this.RunLocationViewTest();
                Task.Delay(this.sleepDuration).Wait();

                this.RunHealthViewTest();
                Task.Delay(this.sleepDuration).Wait();
            }
        }

        private void RunHealthViewTest()
        {
            throw new NotImplementedException("Not supported for legacy V1 API's");
        }

        private void RunLocationViewTest()
        {
            throw new NotImplementedException("Not supported for legacy V1 API's");
        }

        private void RunSearchViewTest()
        {
            throw new NotImplementedException("Not supported for legacy V1 API's");
        }

        private void RunBrowseViewTest()
        {
            throw new NotImplementedException("Not supported for legacy V1 API's");
        }

        #endregion

        private static void HandleTaskResponse(
            Task<HttpResponseMessage> task, ref RequestStatusModel requestStatus, HttpRequestMessage request, FileStream successFile, FileStream errorFile)
        {
            string cv = request.Headers.GetValues("MS-CV").First();

            if (task.IsCanceled)
            {
                Log(errorFile, $"{DateTime.UtcNow.TimeOfDay}: Task canceled, CV: {cv}");
                Interlocked.Increment(ref requestStatus.ErrorCount);
            }
            else if (task.IsFaulted)
            {
                Log(errorFile, $"{DateTime.UtcNow.TimeOfDay}: Task faulted with exception {task.Exception}, CV: {cv}");
                Interlocked.Increment(ref requestStatus.ErrorCount);
            }
            else
            {
                using (HttpResponseMessage response = task.Result)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        Log(successFile, $"{DateTime.UtcNow.TimeOfDay}: Code {response.StatusCode}, CV: {cv}", false);
                        Interlocked.Increment(ref requestStatus.SuccessCount);
                    }
                    else
                    {
                        if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                        {
                            Interlocked.Increment(ref requestStatus.ErrorCount4xx);
                        }
                        else
                        {
                            Interlocked.Increment(ref requestStatus.ErrorCount);
                        }

                        Log(errorFile, $"{DateTime.UtcNow.TimeOfDay}: Code {response.StatusCode}, CV: {cv}");
                    }
                }

                request?.Dispose();
            }
        }

        private static void Log(FileStream file, string content, bool writeToConsole = true)
        {
            if (writeToConsole)
            {
                Console.WriteLine(content);
            }

            var bytes = Encoding.UTF8.GetBytes(content + Environment.NewLine);
            file.Write(bytes, 0, bytes.Length);
        }

        private string RetrieveRandomProxyTicket(UserType userType)
        {
            int randomIndex;

            switch (userType)
            {
                case UserType.View:
                    randomIndex = random.Next(this.userProxyTicketsView.Count - 1);
                    return this.userProxyTicketsView[randomIndex];

                case UserType.Delete:
                    randomIndex = random.Next(this.userProxyTicketsDelete.Count - 1);
                    return this.userProxyTicketsView[randomIndex];

                default:
                    throw new ArgumentOutOfRangeException(nameof(userType), userType, null);
            }
        }

        private string CurrentTestStatus(Stopwatch timer)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Executing test type: {this.requestType} with target RPS: {this.requestsPerSecond}");
            builder.AppendLine($"Time elapsed: {FormatTimeSpan(timer.Elapsed)}");
            var timeLeft = this.testDuration - timer.Elapsed;
            builder.AppendLine($"Time remaining: {FormatTimeSpan(timeLeft.TotalMilliseconds > 0 ? timeLeft : TimeSpan.Zero)}");
            builder.AppendLine();
            builder.AppendLine($"Success count view: {this.SuccessCountView()}, Success count delete: {this.SuccessCountDelete()}");
            builder.AppendLine($"Error count view: {this.ErrorCountView()}, Error count delete: {this.ErrorCountDelete()}");
            builder.AppendLine();
            builder.AppendLine(CurrentTestStatus(this.StatusViewSearch, timer));
            builder.AppendLine(CurrentTestStatus(this.StatusDeleteSearch, timer));
            builder.AppendLine(CurrentTestStatus(this.StatusViewBrowse, timer));
            builder.AppendLine(CurrentTestStatus(this.StatusDeleteBrowse, timer));
            builder.AppendLine(CurrentTestStatus(this.StatusViewHealthLocation, timer));
            builder.AppendLine(CurrentTestStatus(this.StatusViewLocation, timer));
            builder.AppendLine(CurrentTestStatus(this.StatusDeleteLocation, timer));

            builder.AppendLine($"QOS View Overall: {CalculateQoS(this.SuccessCountView(), this.ErrorCountView())}");
            builder.AppendLine($"QOS Delete Overall: {CalculateQoS(this.SuccessCountDelete(), this.ErrorCountDelete())}");
            builder.AppendLine($"RPS View: {this.TotalRequestsView() / timer.Elapsed.TotalSeconds}");
            builder.AppendLine($"RPS Delete: {this.TotalRequestsDelete() / timer.Elapsed.TotalSeconds}");
            builder.AppendLine($"RPS Total: {(this.TotalRequestsView() + this.TotalRequestsDelete()) / timer.Elapsed.TotalSeconds}");
            builder.AppendLine();
            return builder.ToString();
        }

        private static string CurrentTestStatus(RequestStatusModel status, Stopwatch timer)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Api Name: {status.ApiName,10}");

            string qos = $"QOS: {CalculateQoS(status.SuccessCount, status.ErrorCount),5}";
            string rps = $"RPS: {((status.SuccessCount + status.ErrorCount)/timer.Elapsed.TotalSeconds):0.000000,10}";
            builder.AppendLine($"{qos}{string.Empty,5}{rps,15}");

            string requestsTotal = $"Requests Total: {status.TotalCount,10}";
            string success = $"Success: {status.SuccessCount,10}";
            builder.AppendLine($"{requestsTotal}{string.Empty,5}{success,15}");

            const string ErrorTitle = "Error";
            string error = $"{ErrorTitle.PadLeft(11)}: {status.ErrorCount}";
            const string Error4xxTitle = "Error 4xx";
            string error4xx = $"{Error4xxTitle.PadLeft(15)}: {status.ErrorCount4xx,8}";
            builder.AppendLine($"{error,17}{string.Empty,5}{error4xx,11}");
            builder.AppendLine();
            return builder.ToString();
        }

        private double SuccessCountDelete()
        {
            return this.SumSuccessCount(this.StatusDeleteBrowse, this.StatusDeleteSearch, this.StatusDeleteLocation);
        }

        private double SumSuccessCount(params RequestStatusModel[] status)
        {
            return status.Sum(s => s.SuccessCount);
        }

        private double ErrorCountDelete()
        {
            return this.SumErrorCount(this.StatusDeleteBrowse, this.StatusDeleteSearch, this.StatusDeleteLocation);
        }

        private double SumErrorCount(params RequestStatusModel[] status)
        {
            return status.Sum(s => s.ErrorCount);
        }

        private double Error4xxCountDelete()
        {
            return this.SumErrorCount4xx(this.StatusDeleteBrowse, this.StatusDeleteSearch, this.StatusDeleteLocation);
        }

        private double SumErrorCount4xx(params RequestStatusModel[] status)
        {
            return status.Sum(s => s.ErrorCount4xx);
        }

        private double TotalRequestsDelete()
        {
            return this.SuccessCountDelete() + this.ErrorCountDelete() + this.Error4xxCountDelete();
        }

        private double SuccessCountView()
        {
            return this.SumSuccessCount(
                this.StatusViewBrowse, this.StatusViewSearch, this.StatusViewLocation, this.StatusViewHealthLocation);
        }

        private double ErrorCountView()
        {
            return this.SumErrorCount(
                this.StatusViewBrowse, this.StatusViewSearch, this.StatusViewLocation, this.StatusViewHealthLocation);
        }

        private double Error4xxCountView()
        {
            return this.SumErrorCount4xx(
                this.StatusViewBrowse, this.StatusViewSearch, this.StatusViewLocation, this.StatusViewHealthLocation);
        }

        private double TotalRequestsView()
        {
            return this.SuccessCountView() + this.ErrorCountView() + this.Error4xxCountView();
        }

        private static double CalculateQoS(double success, double error)
        {
            if ((success + error) == 0)
            {
                return 0;
            }

            return (Math.Abs(success - error) / (success + error)) * 100;
        }

        private void RedrawScreen(string screenSnapShot)
        {
            Console.Clear();
            Console.Write(screenSnapShot);
        }

        private static string FormatTimeSpan(TimeSpan timespan)
        {
            return $"Days: '{timespan.Days}', " + $"Hours: {timespan.Hours}, " + $"Minutes: {timespan.Minutes}, " + $"Seconds: {timespan.Seconds}, " + $"Milliseconds: {timespan.Milliseconds}";
        }

        private string GetUserProxyTicket(string username, string password)
        {
            password = WebUtility.HtmlEncode(password);

            UserProxyTicketProvider userProxyTicketProvider = new UserProxyTicketProvider(this.rpsConfiguration);
            UserProxyTicketAndPuidResult userTicketResponse = userProxyTicketProvider.GetTicketAndPuidAsync(username, password).Result;

            if (!userTicketResponse.IsSuccess)
            {
                throw new MissingFieldException("Retrieving user proxy ticket failed. ErrorMessage=" + userTicketResponse.ErrorMessage);
            }

            return userTicketResponse.Ticket;
        }
    }
}