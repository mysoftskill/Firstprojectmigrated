import * as angular from "angular";
import { TestSpec, SpyCache } from "../shared-tests/spec.base";

import { ITestableContactService, RequestAdminAssistanceArgs } from "./contact.service";

describe("Contact service", () => {
    let spec: TestSpec;
    let contactService: ITestableContactService;
    let $window: ng.IWindowService;

    beforeEach(() => {
        spec = new TestSpec();

        inject((_contactService_: ITestableContactService, _$window_: ng.IWindowService) => {
            contactService = _contactService_;
            $window = _$window_;
        });
    });

    describe("formatUrl()", () => {
        let bradburySpy: SpyCache<Bradbury.ICorrelationVectorManager>;

        beforeEach(() => {
            bradburySpy = new SpyCache($window.BradburyTelemetry.cv);
            bradburySpy.getFor("getCurrentCvValue").and.returnValue("123%special");

            spec.$state.spies.getFor("href").and.returnValue("https://example.com/current-state-url?query=param&and=another");
        });

        it("formats URL using string template and a property bag, correctly encodes special characters", () => {
            let url = contactService.formatUrl("Test - $URL$ - $CV$ - $TEST$ - $NOTINBAG$", {
                test: "hello world"
            });

            expect(url).toBe("Test - https%3A%2F%2Fexample.com%2Fcurrent-state-url%3Fquery%3Dparam%26and%3Danother - 123%25special - hello%20world - $NOTINBAG$");
        });

        it("prevents some properties from being overridden by callers", () => {
            let url = contactService.formatUrl("Test - $URL$ - $CV$ - $TEST$ - $NOTINBAG$", {
                test: "hello world",
                cv: "original cv must be preserved",
                url: "original URL must be preserved"
            });

            expect(url).toBe("Test - https%3A%2F%2Fexample.com%2Fcurrent-state-url%3Fquery%3Dparam%26and%3Danother - 123%25special - hello%20world - $NOTINBAG$");
        });
    });

    describe("navigateTo()", () => {
        let contactServiceSpy: SpyCache<ITestableContactService>;

        beforeEach(() => {
            contactServiceSpy = new SpyCache(contactService);

            contactServiceSpy.getFor("formatUrl").and.returnValue("https://example.com");
            contactServiceSpy.getFor("navigateTo").and.stub();
        });

        it("is called by collectUserFeedback()", () => {
            contactService.collectUserFeedback();

            expect(contactServiceSpy.getFor("navigateTo")).toHaveBeenCalledWith("https://example.com");
        });

        it("is called by requestAdminAssistance()", () => {
            contactService.requestAdminAssistance("delete-agent", <RequestAdminAssistanceArgs> {});

            expect(contactServiceSpy.getFor("navigateTo")).toHaveBeenCalledWith("https://example.com");
        });
    });
});
