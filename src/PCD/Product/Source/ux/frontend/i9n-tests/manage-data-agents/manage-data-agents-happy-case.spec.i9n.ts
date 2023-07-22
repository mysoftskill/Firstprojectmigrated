import { Test } from "../utilities/test";
import { generateFuzzyGuidFrom } from "../../shared/guid";

describe("On manage data agents page for I9n_Team1", () => {
    beforeAll(() => {
        Test.Navigation.loadPage(`data-agents/manage/${generateFuzzyGuidFrom(1)}`, "manage-data-agents");
    });
    /*
    it("user should see team related information in page context drawer", Test.asyncTest((doneUtil) => {
        Test.Verify.SharedComponent.verifyOwnerContextDrawerForTeamNumber(1, doneUtil);
    }));
    */
    /*
    it("user should see agent list containing I9n_Team1 agents", Test.asyncTest((doneUtil) => {
        Test.Verify.elementPresentWithSelector("div[i9n-data-agent-list]");

        //  Verify agent list section.
        Test.Verify.SharedComponent.verifyAgentListSectionFor({
            entityNumbering: {
                teamNumber: 1,
                agentNumber: 1
            },
            agentsHaveProdConnection: true,
        }, doneUtil);
        Test.Verify.SharedComponent.verifyAgentListSectionFor({
            entityNumbering: {
                teamNumber: 1,
                agentNumber: 2
            },
            agentsHaveProdConnection: true,
        }, doneUtil);

    }));
    */
    it("user should see additional links to register/move data agents", Test.asyncTest((doneUtil) => {
        Test.Verify.elementPresentWithSelector("[i9n-register-data-agent]");
        Test.Verify.linkPointsToLocation(
            Test.Search.elementWithSelector("[i9n-register-data-agent]"),
            `data-agents/create/${generateFuzzyGuidFrom(1)}`,
            doneUtil.addPromiseToDone()
        );

        Test.Verify.elementPresentWithSelector("[i9n-move-data-agents]");
        //  This link opens up the email template, so not verifying anything here.
    }));

    it("user should not see any errors on the page", () => {
        Test.Verify.noErrorOnPage();
    });
});
