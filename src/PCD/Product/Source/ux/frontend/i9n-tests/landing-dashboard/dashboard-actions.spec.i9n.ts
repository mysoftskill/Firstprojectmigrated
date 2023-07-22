import { Test } from "../utilities/test";

xdescribe("On landing dashboard", () => {
    beforeAll(() => {
        // Load the dashboard
        Test.Navigation.loadLandingDashboard();
        Test.Action.waitForTeamPicker();
    });

    describe("when user selects Team2 from team picker", () => {
        beforeAll(() => {
            // Select team with a particular name in the picker.
            let teamPickerInputEl = Test.Search.childElementWithTag(Test.Search.elementWithTag("pcd-team-picker"), "input");
            Test.Action.setText(teamPickerInputEl, "I9n_Team2_Name");
        });
        
        it("user should see Data Assets count for Team2", Test.asyncTest((doneUtil) => {
            let assetGroupSummaryEl = Test.Search.elementWithSelector("[i9n-asset-groups-summary]");
            Test.Verify.elementContainsText(Test.Search.childElementWithSelector(assetGroupSummaryEl, "[i9n-asset-count]"), "2", doneUtil.addPromiseToDone());
        }));
       
        it("user should see Data Agents count for Team2", Test.asyncTest((doneUtil) => {
            let agentsSummaryEl = Test.Search.elementWithSelector("[i9n-agents-summary]");
            Test.Verify.elementContainsText(Test.Search.childElementWithSelector(agentsSummaryEl, "[i9n-agent-count]"), "2", doneUtil.addPromiseToDone());
        }));
    });
});
