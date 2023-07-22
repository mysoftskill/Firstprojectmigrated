import { Test } from "../utilities/test";
import { generateFuzzyGuidFrom } from "../../shared/guid";

describe("On manage team page for I9n_Team1", () => {
    beforeAll(() => {
        Test.Navigation.loadPage(`data-owners/edit/${generateFuzzyGuidFrom(1)}`, "manage-team");
    });

    it("user should see team related information", () => {
        Test.Verify.elementPresentWithTag("pcd-service-tree-summary");

        Test.Verify.elementPresentWithSelector("[i9n-icm-connector-text-field]");
        Test.Verify.elementPresentWithSelector("[i9n-delete-team-button]");

        Test.Verify.elementPresentWithClass("contact-selector");
        Test.Verify.elementPresentWithClass("security-group-selector");
    });

    it("user should not see any errors on the page", () => {
        Test.Verify.noErrorOnPage();
    });

    describe("when user updates team information", () => {
        beforeAll(() => {
            //  Enter write security group in DRS.
            Test.Action.drsSetTextAndClickFirstOption("security-group-selector", "I9n_Team1_SG");

            //  Finally click the save button.
            Test.Action.click(Test.Search.childElementWithTag(Test.Search.elementWithTag("pcd-commit-request-button"), "button"));

            Test.Action.waitForPageNavigation();
        });
        /*
        it("they should not see any errors on the page", () => {
            Test.Verify.noErrorOnPage();
        });
        */
        /*
        it("they should successfully navigate to landing dashboard", () => {
            Test.Verify.elementPresentWithTag("pcd-team-picker");
            Test.Verify.elementPresentWithSelector("[i9n-manage-data-assets]");
            Test.Verify.elementPresentWithSelector("[i9n-manage-data-agents]");
        });
        */
    });
});
