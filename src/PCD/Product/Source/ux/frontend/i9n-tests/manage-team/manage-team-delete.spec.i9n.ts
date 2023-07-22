import { Test } from "../utilities/test";
import { generateFuzzyGuidFrom } from "../../shared/guid";

describe("On manage team page for I9n_Team1", () => {
    beforeAll(() => {
        Test.Navigation.loadPage(`data-owners/edit/${generateFuzzyGuidFrom(1)}`, "manage-team.delete-team");
    });
/*
    it("user should see delete link", () => {
        Test.Verify.elementPresentWithSelector("[i9n-delete-team-button]");
    });
*/
    describe("when user clicks the delete link", () => {
        beforeAll(() => {
            //  Click the delete link.
            Test.Action.click(Test.Search.elementWithSelector("[i9n-delete-team-button]"));

            Test.Action.waitForModal();
        });
/*
        it("they should see a confirmation modal", () => {
            Test.Verify.modalIsShown();
        });
*/
        describe("when user clicks No on that modal", () => {
            beforeAll(() => {
                Test.Action.clickModalNonPrimaryButton();
            });

            it("they should not see any errors on the page", () => {
                Test.Verify.noErrorOnPage();
            });

            it("they should get back to the manage team page", () => {
                Test.Verify.elementPresentWithTag("pcd-service-tree-summary");
                Test.Verify.elementPresentWithSelector("[i9n-delete-team-button]");
            });
        });
    });
});

describe("On manage team page for I9n_Team1", () => {
    beforeAll(() => {
        Test.Navigation.loadPage(`data-owners/edit/${generateFuzzyGuidFrom(1)}`, "manage-team.delete-team");
    });

    it("user should see delete link (2nd call)", () => {
        Test.Verify.elementPresentWithSelector("[i9n-delete-team-button]");
    });

    describe("when user clicks the delete link", () => {
        beforeAll(() => {
            //  Click the delete link.
            Test.Action.click(Test.Search.elementWithSelector("[i9n-delete-team-button]"));

            Test.Action.waitForModal();
        });

        it("they should see a confirmation modal (2nd call)", () => {
            Test.Verify.modalIsShown();
        });

        describe("when user confirms action on that modal", () => {
            beforeAll(() => {
                Test.Action.clickCheckbox(Test.Search.elementWithTag("mee-checkbox"));
                Test.Action.clickModalPrimaryButton();

                Test.Action.waitForPageNavigation();
            });
/*
            it("they should not see any errors on the page", () => {
                Test.Verify.noErrorOnPage();
            });

            it("they should successfully navigate to landing dashboard", () => {
                Test.Verify.elementPresentWithTag("pcd-team-picker");
                Test.Verify.elementPresentWithSelector("[i9n-manage-data-assets]");
                Test.Verify.elementPresentWithSelector("[i9n-manage-data-agents]");
			});
*/
        });
    });
});
