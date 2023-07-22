import { Test } from "../utilities/test";
import { generateFuzzyGuidFrom } from "../../shared/guid";
import { ElementFinder } from "protractor";

describe("On landing dashboard", () => {
    beforeAll(() => {
        // Load the dashboard
        Test.Navigation.loadLandingDashboard();
    });

    describe("user should see general layout of the site with", () => {
        it("UHF header", () => {
            //  TODO: Inspect the mocked out UHF element rather than making real calls. 
            Test.Verify.elementPresentWithId("headerUniversalHeader");
            Test.Verify.elementPresentWithClass("js-global-head"); // UHF header section
        });

        it("content section", () => {
            Test.Verify.elementPresentWithTag("pcd-content");
        });

        it("sidebar section", () => {
            Test.Verify.elementPresentWithTag("pcd-sidebar");
        });

        it("footer", () => {
            Test.Verify.elementPresentWithTag("footer");

            Test.Verify.elementPresentWithSelector("[i9n-footer-app-version]");
            Test.Verify.elementPresentWithSelector("[i9n-footer-cv]");
            Test.Verify.elementPresentWithTag("pcd-feedback");
        });
    });

    describe("user should see Teams section", () => {
        it("top level", () => {
            Test.Verify.elementPresentWithSelector("[i9n-manage-team]");
        });

        it("with team picker and Team1 selected", Test.asyncTest((doneUtil) => {
            Test.Verify.elementPresentWithTag("pcd-team-picker");
            let teamPickerInputEl = Test.Search.childElementWithTag(
                Test.Search.elementWithTag("pcd-team-picker"), "input");

            Test.Verify.inputContainsText(teamPickerInputEl, "I9n_Team1_Name", doneUtil.addPromiseToDone());
        }));

        it("with Modify selected team link", () => {
            Test.Verify.elementPresentWithSelector("[i9n-modify-team-link]");
        });

        it("with Find your team link", () => {
            Test.Verify.elementPresentWithSelector("[i9n-create-team-link]");
        });
    });

    describe("user should see Manage data assets section", () => {
        it("top level", () => {
            Test.Verify.elementPresentWithSelector("[i9n-manage-data-assets]");
        });

        it("with data asset count", Test.asyncTest((doneUtil) => {
            let assetGroupSummaryEl = Test.Search.elementWithSelector("[i9n-asset-groups-summary]");
            Test.Verify.elementContainsText(
                Test.Search.childElementWithSelector(assetGroupSummaryEl, "[i9n-asset-count]"), "1", doneUtil.addPromiseToDone());
        }));

        it("with Register data asset link", () => {
            Test.Verify.elementPresentWithSelector("[i9n-register-data-asset]");
        });
    });

    describe("user should see Manage data agents section", () => {
        it("top level", () => {
            Test.Verify.elementPresentWithSelector("[i9n-manage-data-agents]");
        });

        it("with data agent count", Test.asyncTest((doneUtil) => {
            let agentsSummary = Test.Search.elementWithSelector("[i9n-agents-summary]");
            Test.Verify.elementContainsText(
                Test.Search.childElementWithSelector(agentsSummary, "[i9n-agent-count]"), "1", doneUtil.addPromiseToDone());
        }));

        it("with Register data agent link", () => {
            Test.Verify.elementPresentWithSelector("[i9n-register-data-agent]");
        });
    });

    describe("links should point to the right locations for", () => {
        it("team management", Test.asyncTest((doneUtil) => {
            Test.Verify.linkPointsToLocation(Test.Search.elementWithSelector("[i9n-create-team-link]"),
                "data-owners/create/service-tree", doneUtil.addPromiseToDone());                 
            Test.Verify.linkPointsToLocation(Test.Search.elementWithSelector("[i9n-modify-team-link]"),
                `data-owners/edit/${generateFuzzyGuidFrom(1)}`, doneUtil.addPromiseToDone());
        }));

        it("data asset management", Test.asyncTest((doneUtil) => {
            Test.Verify.linkPointsToLocation(Test.Search.elementWithSelector("[i9n-manage-data-assets-link]"),
                `data-assets/manage/${generateFuzzyGuidFrom(1)}`, doneUtil.addPromiseToDone());
            Test.Verify.linkPointsToLocation(Test.Search.elementWithSelector("[i9n-register-data-asset]"),
                `data-assets/create/${generateFuzzyGuidFrom(1)}`, doneUtil.addPromiseToDone());
        }));

        it("data agent management", Test.asyncTest((doneUtil) => {
            Test.Verify.linkPointsToLocation(Test.Search.elementWithSelector("[i9n-manage-data-agents-link]"),
                `data-agents/manage/${generateFuzzyGuidFrom(1)}`, doneUtil.addPromiseToDone());
            Test.Verify.linkPointsToLocation(Test.Search.elementWithSelector("[i9n-register-data-agent]"),
                `data-agents/create/${generateFuzzyGuidFrom(1)}`, doneUtil.addPromiseToDone());
        }));
    });

});
