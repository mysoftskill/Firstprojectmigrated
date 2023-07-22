using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Osgs.Infra.Monitoring.AspNetCore;
using Microsoft.PrivacyServices.UX.Configuration;
using Microsoft.PrivacyServices.UX.Core.ClientProviderAccessor;
using Microsoft.PrivacyServices.UX.Core.PdmsClient;
using Microsoft.PrivacyServices.UX.Core.PxsClient;
using Microsoft.PrivacyServices.UX.Core.ServiceTreeClient;
using Microsoft.PrivacyServices.UX.Core.UhfClient;

namespace Microsoft.PrivacyServices.UX.Controllers
{

    [HandleAjaxErrors(ErrorCode = "generic")]
    public class HealthCheckController : Controller
    {

        private readonly IPdmsClientConfig pdmsConfig;

        private readonly IPxsClientConfig pxsConfig;

        private readonly IClientProviderAccessor<IServiceTreeClientProvider> serviceTreeClientProviderAccessor;

        private readonly IUhfClientConfig uhfConfig;

        private readonly HttpClient client;

        public HealthCheckController(
            IClientProviderAccessor<IServiceTreeClientProvider> serviceTreeClientProviderAccessorParam,
            IUhfClient uhfClientParam,
            IPdmsClientConfig pdmsConfigParam,
            IPxsClientConfig pxsConfigParam,
            IUhfClientConfig uhfConfigParam
            )
        {
            client = new HttpClient();
            pdmsConfig = pdmsConfigParam ?? throw new System.ArgumentNullException(nameof(pdmsConfigParam));
            pxsConfig = pxsConfigParam ?? throw new System.ArgumentNullException(nameof(pxsConfigParam));
            uhfConfig = uhfConfigParam ?? throw new System.ArgumentNullException(nameof(uhfConfigParam));
            serviceTreeClientProviderAccessor = serviceTreeClientProviderAccessorParam ?? throw new System.ArgumentNullException(nameof(serviceTreeClientProviderAccessorParam));
        }

        [HttpGet]
        public IActionResult healthcheck()
        {
            List<string> faultyServicesList = new List<string>();
            int statusCode = (int)HttpStatusCode.OK;

            if (!checkHealthGeneric(pdmsConfig.Endpoint + "/keepalive"))
            {
                statusCode = (int)HttpStatusCode.InternalServerError;
                faultyServicesList.Add("PDMS");
            }

            if (!checkHealthGeneric(pxsConfig.ApiEndpoint + "/keepalive"))
            {
                statusCode = (int)HttpStatusCode.InternalServerError;
                faultyServicesList.Add("PXS");
            }

            if (serviceTreeClientProviderAccessor.ProviderInstance == null || serviceTreeClientProviderAccessor.ProviderInstance.Instance == null)
            {
                statusCode = (int)HttpStatusCode.InternalServerError;
                faultyServicesList.Add("ServiceTree");
            }

            if (!checkHealthGeneric(uhfConfig.ServiceEndpoint + "/keepalive"))
            {
                statusCode = (int)HttpStatusCode.InternalServerError;
                faultyServicesList.Add("UHF");
            }

            string responseMessage = "";

            if (faultyServicesList.Count>0)
            {
                responseMessage = string.Join(", ", faultyServicesList) + " - not available";
            }

            return StatusCode(statusCode, responseMessage);
        }

        private bool checkHealthGeneric(string url)
        {
            try
            {
                HttpResponseMessage result = client.GetAsync(url).Result;
                HttpStatusCode StatusCode = result.StatusCode;
                if (StatusCode == HttpStatusCode.OK)
                {
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
            return false;
        }
    }
}
