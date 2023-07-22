import { Test } from "../utilities/test";
import { generateFuzzyGuidFrom } from "../../shared/guid";

describe("On register team page", () => {
    beforeAll(() => {
        Test.Navigation.loadPage("data-owners/create/service-tree", "register-team.team-already-exists");
    });

    describe("when an admin enters a team which already exists", () => {
        beforeAll(() => {
            //  Enter "I9n_Team2" in team selector.
            let teamSelectorInputEl = Test.Search.childElementWithTag(
                Test.Search.elementWithSelector("[i9n-service-tree-selector]"), "input");
            Test.Action.setText(teamSelectorInputEl, "I9n_Team2");

            //  Select the option in dropdown suggestions.
            Test.Action.clickFirstDrsOption("service-tree-selector-div");
        });
        /*
        it("they should not see any errors on the page", () => {
            Test.Verify.noErrorOnPage();
        });
        */
        /*
        it("they should see an alert banner with a link to edit team and disabled save button", Test.asyncTest((doneUtil) => {
            Test.Verify.elementPresentWithSelector("[i9n-team-exists-alert]");
            Test.Verify.linkPointsToLocation(
                Test.Search.childElementWithTag(Test.Search.elementWithSelector("[i9n-team-exists-alert]"), "a"),
                `data-owners/edit/${generateFuzzyGuidFrom(2)}`,
                doneUtil.addPromiseToDone()
            );

            Test.Verify.elementIsDisabled(
                Test.Search.childElementWithTag(Test.Search.elementWithTag("pcd-commit-request-button"), "button")
            );
        }));
        */
    });
});
