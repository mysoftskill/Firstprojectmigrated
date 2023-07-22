namespace Microsoft.PrivacyServices.CommandFeed.CustomActivity
{
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using System;
    using System.Net.Http;

    public class ConsoleLogger : CommandFeedLogger
    {
        public enum LogLevel
        {
            Information,
            Warning,
            Error,
        };

        public static void Log(string message, LogLevel logLevel = LogLevel.Information)
        {
            var timeStamp = DateTime.Now.ToString("MM-dd HH:mm:ss");
            Console.WriteLine($"[{timeStamp}] [{logLevel}] {message}");
        }

        public static void LogException(Exception ex)
        {
            Log($"Exception: {ex.Message}\n {ex.StackTrace}\n {ex.InnerException}", LogLevel.Error);
        }

        public override void HttpResponseReceived(HttpRequestMessage request, HttpResponseMessage response)
        {
            Log($"HTTP Response Received! Url = {request.RequestUri}, ResponseCode = {response.StatusCode}, ResponseBody = {response.Content.ReadAsStringAsync().Result}");
        }

        public override void UnhandledException(Exception ex)
        {
            LogException(ex);
        }

        public override void BeginServiceToServiceAuthRefresh(string targetSiteName, long siteId)
        {
            Log($"Beginning S2S refresh: {targetSiteName}, {siteId}");
        }

        public override void CommandValidationException(string cv, string commandId, Exception ex)
        {
            Log($"[CommandValidationException] correlationVector: {cv} commandId: {commandId}", LogLevel.Error);
            LogException(ex);
        }
    }
}
