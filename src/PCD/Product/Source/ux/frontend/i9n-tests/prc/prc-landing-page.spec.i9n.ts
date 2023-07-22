import { Test } from "../utilities/test";

describe("On manual requests landing page", () => {
    beforeAll(() => {
        Test.Navigation.loadPage("manual-requests", "manual-requests");
    });

    it("user should see a valid Delete link", Test.asyncTest((doneUtil) => {
        Test.Verify.elementPresentWithSelector("[i9n-delete-link]");
        Test.Verify.linkPointsToLocation(Test.Search.elementWithSelector("[i9n-delete-link]"),
            "manual-requests/delete",
            doneUtil.addPromiseToDone()
        );
    }));

    it("user should see a valid Export link", Test.asyncTest((doneUtil) => {
        Test.Verify.elementPresentWithSelector("[i9n-export-link]");
        Test.Verify.linkPointsToLocation(Test.Search.elementWithSelector("[i9n-export-link]"),
            "manual-requests/export",
            doneUtil.addPromiseToDone()
        );
    }));

    it("user should see a valid Status link", Test.asyncTest((doneUtil) => {
        Test.Verify.elementPresentWithSelector("[i9n-status-link]");
        Test.Verify.linkPointsToLocation(Test.Search.elementWithSelector("[i9n-status-link]"),
            "manual-requests/status",
            doneUtil.addPromiseToDone()
        );
    }));
});
