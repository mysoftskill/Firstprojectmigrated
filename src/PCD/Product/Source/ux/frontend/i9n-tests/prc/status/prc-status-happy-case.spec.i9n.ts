import { Test } from "../../utilities/test";
import { generateFuzzyGuidFrom } from "../../../shared/guid";
import { PrivacyRequestType } from "../../../shared/manual-requests/manual-request-types";

describe("On manual requests status page", () => {
    const exportRequestIdBase = 1;
    const deleteRequestIdBase = 2;
    const accountCloseRequestIdBase = 3;

    beforeAll(() => {
        Test.Navigation.loadPage("manual-requests/status", "manual-requests.status");
    });

    it("user should not see any errors on the page", () => {
        Test.Verify.noErrorOnPage();
    });

    it("user should see the request type picker and search bar", () => {
        Test.Verify.elementPresentWithSelector("[i9n-select-request-type]");
        Test.Verify.elementPresentWithSelector("[i9n-search-request]");
    });

    it("user should see the Export requests by default", () => {
        const multiplier = exportRequestIdBase * 10;

        Test.Verify.elementPresentWithSelector(`[i9n-request-id="${generateFuzzyGuidFrom(multiplier + 1)}"]`);
        Test.Verify.elementPresentWithSelector(`[i9n-request-id="${generateFuzzyGuidFrom(multiplier + 2)}"]`);
    });

    describe("when user switches the request type to Delete", () => {
        let multiplier: any;

        beforeEach(() => {
            multiplier = deleteRequestIdBase * 10;
        });

        beforeAll(Test.asyncTest((doneUtil) => {
            Test.Action.clickMeeSelectOptionFor("[i9n-select-request-type]", PrivacyRequestType[PrivacyRequestType.Delete], doneUtil);
        }));

        it("user should not see any errors on the page", () => {
            Test.Verify.noErrorOnPage();
        });

        it("user should see the Delete requests", () => {
            Test.Verify.elementPresentWithSelector(`[i9n-request-id="${generateFuzzyGuidFrom(multiplier + 1)}"]`);
            Test.Verify.elementPresentWithSelector(`[i9n-request-id="${generateFuzzyGuidFrom(multiplier + 2)}"]`);
        });
    });

    describe("when user switches the request type to Account close", () => {
        let multiplier: any;

        beforeEach(() => {
            multiplier = accountCloseRequestIdBase * 10;
        });

        beforeAll(Test.asyncTest((doneUtil) => {
            Test.Action.clickMeeSelectOptionFor("[i9n-select-request-type]", PrivacyRequestType[PrivacyRequestType.AccountClose], doneUtil);
        }));

        it("user should not see any errors on the page", () => {
            Test.Verify.noErrorOnPage();
        });

        it("user should see the Account close requests", () => {
            Test.Verify.elementPresentWithSelector(`[i9n-request-id="${generateFuzzyGuidFrom(multiplier + 1)}"]`);
            Test.Verify.elementPresentWithSelector(`[i9n-request-id="${generateFuzzyGuidFrom(multiplier + 2)}"]`);
        });
    });
});
