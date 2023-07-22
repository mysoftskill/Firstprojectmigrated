namespace PCF.FunctionalTests
{
    using System;
    using Xunit.Abstractions;
    using System.Net.Http;

    public abstract class FunctionalTestBase
    {
        protected FunctionalTestBase(ITestOutputHelper outputHelper)
        {
            this.OutputHelper = outputHelper;
        }

        protected ITestOutputHelper OutputHelper { get; }

        protected void Log(string message)
        {
            this.OutputHelper.WriteLine(message);
        }

        protected void Log(HttpResponseMessage response)
        {
            this.OutputHelper.WriteLine(string.Empty);
            this.OutputHelper.WriteLine($"Received response from {response.RequestMessage.Method} {response.RequestMessage.RequestUri}");
            this.OutputHelper.WriteLine($"StatusCode={response.StatusCode}");

            foreach (var header in response.Headers)
            {
                this.OutputHelper.WriteLine($"Header: {header.Key} = {string.Join(";", header.Value)}");
            }

            this.OutputHelper.WriteLine(string.Empty);
            this.OutputHelper.WriteLine(response.Content.ReadAsStringAsync().Result);
        }
    }
}
