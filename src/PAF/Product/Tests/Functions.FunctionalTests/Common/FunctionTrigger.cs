namespace Microsoft.PrivacyServices.AzureFunctions.FunctionalTests.Common
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.AzureFunctions.Common.Configuration;
    using Microsoft.PrivacyServices.Common.Azure;
    using Newtonsoft.Json;

    public class FunctionTrigger
    {
        private const string ComponentName = nameof(FunctionTrigger);

        private readonly IFunctionLocalConfiguration config;
        private readonly ILogger logger;
        private readonly HttpClient httpClient;

        public FunctionTrigger(ILogger logger, IFunctionLocalConfiguration config)
        {
            this.config = config;
            this.logger = logger ?? throw new ArgumentException(nameof(logger));

            this.httpClient = new HttpClient // lgtm [cs/httpclient-checkcertrevlist-disabled]
            {
                BaseAddress = new Uri(config.PafFunctionUrl)
            };

            var defaultRequestHeaders = this.httpClient.DefaultRequestHeaders;
            defaultRequestHeaders.Add("Accept", "application/json");

            var key = config.PafFunctionKey;
            if (!string.IsNullOrEmpty(key))
            {
                defaultRequestHeaders.Add("x-functions-key", key);

                this.logger.Information(ComponentName, $"FunctionTrigger: using key: {key.Substring(0, 6)}");
            }
        }

        public async Task<bool> InvokeAsync<T>(string apiUrl, T payload)
        {
            string serializedPayload = string.Empty;
            if (payload != null)
            {
                serializedPayload = JsonConvert.SerializeObject(payload);
            }

            var input = new FunctionInvocation(serializedPayload);
            string serializedObject = JsonConvert.SerializeObject(input);
            StringContent content = new StringContent(serializedObject, System.Text.Encoding.UTF8, "application/json");

            this.logger.Information(ComponentName, $"FunctionTrigger: url: {this.httpClient.BaseAddress}/{apiUrl}, input: {serializedPayload}");

            bool success = false;
            try
            {
                using (var response = await this.httpClient.PostAsync(apiUrl, content).ConfigureAwait(false))
                {
                    success = response.IsSuccessStatusCode;
                    if (!success)
                    {
                        this.logger.Information(ComponentName, $"FunctionTrigger: Call to function failed: {response.StatusCode}");
                    }
                }
            }
            catch (Exception e)
            {
                this.logger.Error(ComponentName, e, $"Error triggering function: {this.httpClient.BaseAddress}/{apiUrl}");
            }

            return success;
        }

        public class FunctionInvocation
        {
            public FunctionInvocation(string input)
            {
                this.Input = input;
            }

            public string Input { get; set; }
        }
    }
}
