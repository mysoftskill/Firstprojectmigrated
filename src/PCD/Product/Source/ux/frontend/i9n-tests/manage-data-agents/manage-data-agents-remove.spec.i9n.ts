import { Test } from "../utilities/test";
import { generateFuzzyGuidFrom } from "../../shared/guid";
import { ElementFinder } from "protractor";

describe("On manage data agents page for I9n_Team1", () => {
    let agent1RowEl: ElementFinder;

    beforeAll(() => {
        Test.Navigation.loadPage(`data-agents/manage/${generateFuzzyGuidFrom(1)}`, "manage-data-agents.remove-agent");
    });

    it("user should see Remove link for I9n_Agent1_Team1", () => {
        agent1RowEl = Test.Search.elementWithId(generateFuzzyGuidFrom(1));

        Test.Verify.childElementPresentWithSelector(
            Test.Search.childElementWithSelector(agent1RowEl, "[i9n-action-list]"), "[i9n-remove-agent]");
    });

    describe("when user clicks the Remove link", () => {
        beforeAll(() => {
            Test.Action.click(Test.Search.childElementWithSelector(
                Test.Search.childElementWithSelector(agent1RowEl, "[i9n-action-list]"), "[i9n-remove-agent]"));

            Test.Action.waitForModal();
        });

        it("they should see a confirmation modal", () => {
            Test.Verify.modalIsShown();
        });

        describe("when user clicks No on the modal", () => {
            beforeAll(() => {
                Test.Action.clickModalNonPrimaryButton();
            });

            it("they should not see any errors on the page", () => {
                Test.Verify.noErrorOnPage();
            });

            it("they should get back to the manage data agents page and nothing should be removed", Test.asyncTest((doneUtil) => {
                Test.Verify.elementPresentWithTag("pcd-manage-data-agents");

                //  Verify agent list sections.
                Test.Verify.SharedComponent.verifyAgentListSectionFor({
                    entityNumbering: {
                        teamNumber: 1,
                        agentNumber: 1
                    },
                    agentsHaveProdConnection: false,
                }, doneUtil);
                Test.Verify.SharedComponent.verifyAgentListSectionFor({
                    entityNumbering: {
                        teamNumber: 1,
                        agentNumber: 2
                    },
                    agentsHaveProdConnection: false,
                }, doneUtil);
            }));
        });
    });
});

describe("On manage data agents page for I9n_Team1", () => {
    let agent1RowEl: ElementFinder;

    beforeAll(() => {
        Test.Navigation.loadPage(`data-agents/manage/${generateFuzzyGuidFrom(1)}`, "manage-data-agents.remove-agent");
    });
    /*
    it("user should see Remove link for I9n_Agent1_Team1 (2nd call)", () => {
        agent1RowEl = Test.Search.elementWithId(generateFuzzyGuidFrom(1));

        Test.Verify.childElementPresentWithSelector(
            Test.Search.childElementWithSelector(agent1RowEl, "[i9n-action-list]"), "[i9n-remove-agent]");
    });
    */
    describe("when user clicks the Remove link", () => {
        beforeAll(() => {
            Test.Action.click(Test.Search.childElementWithSelector(
                Test.Search.childElementWithSelector(agent1RowEl, "[i9n-action-list]"), "[i9n-remove-agent]"));

            Test.Action.waitForModal();
        });
        /*
        it("they should see a confirmation modal (2nd call)", () => {
            Test.Verify.modalIsShown();
        });
        /*
        /*
        xdescribe("when user confirms action on the modal", () => {
            beforeAll(() => {
                Test.Action.clickModalPrimaryButton();
            });

            it("they should not see any errors on the page", () => {
                Test.Verify.noErrorOnPage();
            });

            it("they should get back to the manage data agents page and see I9n_Agent1_Team1 removed", Test.asyncTest((doneUtil) => {
                Test.Verify.elementPresentWithTag("pcd-manage-data-agents");

                //  Verify agent list sections.
                Test.Verify.elementAbsentWithId(generateFuzzyGuidFrom(1));
                Test.Verify.SharedComponent.verifyAgentListSectionFor({
                    entityNumbering: {
                        teamNumber: 1,
                        agentNumber: 2
                    },
                    agentsHaveProdConnection: false,
                }, doneUtil);
            }));
        });
        */
    });
});
