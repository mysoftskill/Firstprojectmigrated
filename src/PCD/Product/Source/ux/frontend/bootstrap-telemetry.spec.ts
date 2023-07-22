import { scrubUrl, overrideRequestQosData } from "./bootstrap-telemetry";

let Sensitive_Param = "PII_INPUT";

let targetUri = "";
let sanitizedUri = "";

describe("URL scrubber", () => {
    describe("MS Graph API", () => {
        it("removes display name filter parameters from targetUri", () => {
            [targetUri, sanitizedUri] =
                getSanitizingUrls`https://graph.microsoft.com/v1.0/groups?$filter=startswith(displayName%2C%20%27${Sensitive_Param}%27)&_=1508862116445`;
            expect(scrubUrl(targetUri)).toBe(sanitizedUri);

            [targetUri, sanitizedUri] =
                getSanitizingUrls`https://graph.microsoft.com/v1.0/groups?$filter=startswith(displayName%2C%20'${Sensitive_Param}')&_=1508862116445`;
            expect(scrubUrl(targetUri)).toBe(sanitizedUri);
        });

        it("removes mail filter parameters from targetUri", () => {
            [targetUri, sanitizedUri] =
                getSanitizingUrls`https://graph.microsoft.com/v1.0/groups?$filter=startswith(mail%2C%20%27${Sensitive_Param}%27)&_=1508862116460`;
            expect(scrubUrl(targetUri)).toBe(sanitizedUri);

            [targetUri, sanitizedUri] =
                getSanitizingUrls`https://graph.microsoft.com/v1.0/groups?$filter=startswith(mail%2C%20'${Sensitive_Param}')&_=1508862116460`;
            expect(scrubUrl(targetUri)).toBe(sanitizedUri);
        });

        it("removes userPrincipalName filter parameters from targetUri", () => {
            [targetUri, sanitizedUri] =
                getSanitizingUrls`https://graph.microsoft.com/v1.0/users?$filter=startswith(userPrincipalName%2C%20%27${Sensitive_Param}%27)&_=1508862116458`;
            expect(scrubUrl(targetUri)).toBe(sanitizedUri);

            [targetUri, sanitizedUri] =
                getSanitizingUrls`https://graph.microsoft.com/v1.0/users?$filter=startswith(userPrincipalName%2C%20'${Sensitive_Param}')&_=1508862116458`;
            expect(scrubUrl(targetUri)).toBe(sanitizedUri);
        });

        it("removes id filter parameters from targetUri", () => {
            [targetUri, sanitizedUri] =
                getSanitizingUrls`https://graph.microsoft.com/v1.0/groups?$filter=id%20eq%20%27${Sensitive_Param}%27&_=1508862116464`;
            expect(scrubUrl(targetUri)).toBe(sanitizedUri);

            [targetUri, sanitizedUri] =
                getSanitizingUrls`https://graph.microsoft.com/v1.0/groups?$filter=id%20eq%20'${Sensitive_Param}'&_=1508862116464`;
            expect(scrubUrl(targetUri)).toBe(sanitizedUri);
        });
    });
});

describe("overrideRequestQosData", () => {
    it("classifies client QoS data correctly", () => {
        let request = <Bradbury.JQueryTelemetryAjaxSettings>{};

        //  The list of status codes is not exhaustive.
        let goodCodes = [0, 100, 101, 102, 200, 201, 202, 300, 301, 302, 400, 403, 404];
        for (let code of goodCodes) {
            let response = <JQueryXHR>{ status: code };
            let qosData = <Bradbury.RequestQosData>{};

            overrideRequestQosData(request, response, qosData);
            expect(qosData.isSuccess).toBe(true);
        }

        //  The list of status codes is not exhaustive.
        let badCodes = [401, 402, 405, 408 /* explicit timeout */, 412, 418, 421, 500, 501, 502, 503];
        for (let code of badCodes) {
            let response = <JQueryXHR>{ status: code };
            let qosData = <Bradbury.RequestQosData>{};

            overrideRequestQosData(request, response, qosData);
            expect(qosData.isSuccess).toBe(false);
        }
    });

    it("classifies client QoS data as non-QoS impacting, if response.status is falsy", () => {
        let request = <Bradbury.JQueryTelemetryAjaxSettings>{};

        let response = <JQueryXHR>{};
        let qosData = <Bradbury.RequestQosData>{};

        overrideRequestQosData(request, response, qosData);
        expect(qosData.isSuccess).toBe(true);
    });

    it("classifies client QoS data as QoS impacting, if response.status is falsy and timeout was detected", () => {
        let request = <Bradbury.JQueryTelemetryAjaxSettings><any>{ timeout: 30000 };

        let response = <JQueryXHR>{};
        let qosData = <Bradbury.RequestQosData><any>{ latencyMs: 30000 };

        overrideRequestQosData(request, response, qosData);
        expect(qosData.isSuccess).toBe(false);
    });
});

// Helper to generate the target URI containing sensitive information, as well as the expected sanitized URI 
function getSanitizingUrls(templateUrl: TemplateStringsArray, sensitiveData: string): [string, string] {
    return [templateUrl[0] + sensitiveData + templateUrl[1], templateUrl[0] + "REMOVED" + templateUrl[1]];
}
