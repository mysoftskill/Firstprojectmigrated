/// <reference path="../wwwroot/bradbury/lib/bradbury-lib.d.ts"/>

//  Result of telemetry bootstrapper.
export type BootstrapTelemetryResult = {
    //  Instance of the correlation context manager. 
    correlationContext: Bradbury.ICorrelationContextManager;

    // Instance of the telemetry provider.
    telemetryProvider: Bradbury.ITelemetryProvider;
};

/**
 * Creates and configures telemetry provider instance.
 * @param appId JSLL app ID.
 */
export function bootstrapTelemetry(appId: string): BootstrapTelemetryResult {
    let provider = new Bradbury.TelemetryProvider({
        //  We control when page views are fired (see app.routing for more details).
        allowAutoPageView: false,
        flights: []
    });

    let jsllOptions: Bradbury.JsllTelemetryOptions = {
        appId,
        allowClickTracking: true,
        ambientCookieName: "PCD-CV",
        urlScrubber: scrubUrl,
        overrideRequestQosData: overrideRequestQosData,
        flights: []
    };

    let cvManager = new Bradbury.Jsll4.CorrelationVectorManager(jsllOptions);
    provider.setCorrelationVectorManager(cvManager);

    let ccManager = new Bradbury.Jsll4.CorrelationContextManager({});

    let jsll = new Bradbury.Jsll4.ClientTelemetrySink(jsllOptions, cvManager);
    provider.registerBiSink(jsll);
    provider.registerQosSink(jsll);
    provider.registerScenariosSink(jsll);

    let ajaxTelemetry = new Bradbury.Jsll4.AjaxTelemetrySink(jsllOptions, cvManager, ccManager);
    provider.setAjaxTelemetrySink(ajaxTelemetry);

    provider.useAsGlobalTelemetryProvider();

    return {
        correlationContext: ccManager,
        telemetryProvider: provider.toTelemetryProvider()
    };
}

/**
 * Cleans up sensitive data from the URL.
 * @param url Original URL value.
 */
export function scrubUrl(url: string): string {
    url = url.replace(/\b(displayName|mail|userPrincipalName)(%2C%20(%27|'))([^\&$]+)(%27|')/ig, "$1$2REMOVED$5");
    url = url.replace(/\b(id)(%20eq%20(%27|'))([^\&$]+)(%27|')/ig, "$1$2REMOVED$5");
    return url;
}

/**
 * Overrides request QoS data, based on the response.
 * @param request Request that was made.
 * @param response Response that was received.
 * @param qosData QoS data that is collected so far.
 */
export function overrideRequestQosData(request: Bradbury.JQueryTelemetryAjaxSettings, response: JQueryXHR, qosData: Bradbury.RequestQosData): void {
    let responseStatus = response.status || 0;

    //  If request times out, jQuery doesn't set response.status value. Treat this situation as an implicit timeout.
    if (!responseStatus && request.timeout && qosData.latencyMs && qosData.latencyMs >= request.timeout) {
        responseStatus = 408;                           //  Request timeout.
        qosData.isSuccess = false;                      //  Classify implicit timeout as a QoS-impacting failure.

        return;
    }

    //  Use status code to determine whether the response is QoS impacting for us.
    qosData.isSuccess = responseStatus < 401 || 403 === responseStatus || 404 === responseStatus;
}
