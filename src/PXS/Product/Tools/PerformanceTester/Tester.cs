//--------------------------------------------------------------------------------
// <copyright file="IMemberClient.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Tools.PerformanceTester
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    public class Tester
    {
        private int ServicePointConnectionLimit { get; set; }

        private int ServicePointIdleTimeout { get; set; }

        private TimeSpan HttpClientTimeout { get; set; }

        public Tester(int connectionLimit, int idleTimeout, TimeSpan clientTimeout)
        {
            this.ServicePointConnectionLimit = connectionLimit;
            this.ServicePointIdleTimeout = idleTimeout;
            this.HttpClientTimeout = clientTimeout;
        }

        public async Task Execute(
            int requestsPerSecond, int durationInSeconds, HttpMethod method, string endpoint, IDictionary<string, string> headers, string certificateThumbprint)
        {
            Console.WriteLine("Request Method: {0}", method);
            Console.WriteLine("Request URL: {0}", endpoint);
            Console.WriteLine("Request Headers: {0}", JsonConvert.SerializeObject(headers));
            Console.WriteLine("Request Certificate Thumbprint: {0}", certificateThumbprint);
            Console.WriteLine();
            Console.WriteLine("RequestsPerSecond: {0}", requestsPerSecond);
            Console.WriteLine("DurationInSeconds: {0}", durationInSeconds);
            Console.WriteLine();

            int millisecondsBetweenRequests = 1000 / requestsPerSecond;
            int totalRequests = requestsPerSecond * durationInSeconds;

            List<HttpRequestMessage> requests = new List<HttpRequestMessage>();
            List<Task> tasks = new List<Task>();
            using (HttpClientHandler handler = new HttpClientHandler())
            using (HttpClient client = new HttpClient(handler)) // lgtm [cs/httpclient-checkcertrevlist-disabled]
            {
                //using below query identifier for suppressing CodeQL Error for Certificate validation disabled.
                //Test scenarios do not need cert validation.
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; }; // lgtm[cs/do-not-disable-cert-validation]
                ServicePointManager.DefaultConnectionLimit = this.ServicePointConnectionLimit;
                ServicePointManager.MaxServicePointIdleTime = this.ServicePointIdleTimeout;
                X509Certificate2 certificate = Tester.GetCertificate(certificateThumbprint);
                client.Timeout = this.HttpClientTimeout;
                handler.ClientCertificates.Add(certificate);

                try
                {
                    // Create the requests
                    for (int k = 0; k < totalRequests; k++)
                    {
                        // Modify the endpoint if you want to target a different machine/environment
                        HttpRequestMessage request = new HttpRequestMessage(method, endpoint);
                        foreach (var header in headers)
                        {
                            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }
                        requests.Add(request);
                    }

                    // Execute the requests
                    for (int i = 0; i < durationInSeconds; i++)
                    {
                        for (int j = 0; j < requestsPerSecond; j++)
                        {
                            int index = (i * requestsPerSecond) + j;
                            HttpRequestMessage request = requests[index];

                            Console.WriteLine("{0}_{1}: Executing", DateTime.UtcNow.TimeOfDay, index);
                            Task task = client.SendAsync(request).ContinueWith((anticedent) =>
                            {
                                if (anticedent.IsCanceled)
                                {
                                    Console.WriteLine("{0}_{1}: Task canceled", DateTime.UtcNow.TimeOfDay, index);
                                }
                                else if (anticedent.IsFaulted)
                                {
                                    Console.WriteLine("{0}_{1}: Task faulted with exception {2}", DateTime.UtcNow.TimeOfDay, index, anticedent.Exception);
                                }
                                else
                                {
                                    using (HttpResponseMessage response = anticedent.Result)
                                    {
                                        Console.WriteLine("{0}_{1}: Complete with Status Code {2}", DateTime.UtcNow.TimeOfDay, index, response.StatusCode);
                                    }
                                }
                            });
                            tasks.Add(task);

                            Thread.Sleep(millisecondsBetweenRequests);
                        }
                    }
                    await Task.WhenAll(tasks);
                }
                finally
                {
                    foreach (HttpRequestMessage request in requests)
                    {
                        request.Dispose();
                    }
                }
            }
        }

        private static X509Certificate2 GetCertificate(string thumbprint)
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);

            try
            {
                store.Open(OpenFlags.ReadOnly);

                var foundCertificates = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
                return foundCertificates[0];
            }
            finally
            {
                store.Close();
            }
        }
    }
}
