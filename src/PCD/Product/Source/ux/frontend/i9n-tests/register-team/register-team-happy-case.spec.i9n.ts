import { Test } from "../utilities/test";

describe("On register team page", () => {
    beforeAll(() => {
        Test.Navigation.loadPage("data-owners/create/service-tree", "register-team");
    });

    describe("when user enters all information correctly", () => {
        beforeAll(() => {
            //  Enter "I9n_Team3" in team selector.
            let teamSelectorInputEl = Test.Search.childElementWithTag(Test.Search.elementWithSelector("[i9n-service-tree-selector]"), "input");
            Test.Action.setText(teamSelectorInputEl, "I9n_Team3");

            //  Select the option in dropdown suggestions.
            Test.Action.clickFirstDrsOption("service-tree-selector-div");

            //  Enter write security group in DRS.
            Test.Action.drsSetTextAndClickFirstOption("security-group-selector", "I9n_Team3_SG");

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
