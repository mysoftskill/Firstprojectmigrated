import { Test } from "../utilities/test";
import { generateFuzzyGuidFrom } from "../../shared/guid";
import { ElementFinder } from "protractor";

describe("On manage data assets page for I9n_Team1", () => {
    let asset1RowEl: ElementFinder;

    beforeAll(() => {
        Test.Navigation.loadPage(`data-assets/manage/${generateFuzzyGuidFrom(1)}`, "manage-data-assets.remove-asset");
    });
    /*
    it("user should see remove link for I9n_Asset1_Team1", () => {
        asset1RowEl = Test.Search.elementWithId(generateFuzzyGuidFrom(1));

        Test.Verify.childElementPresentWithSelector(
            Test.Search.childElementWithSelector(asset1RowEl, "[i9n-action-list]"), "[i9n-remove-asset]");
    });
    */
    describe("when user clicks the remove link", () => {
        beforeAll(() => {
            //  Click the remove link.
            Test.Action.click(Test.Search.childElementWithSelector(
                Test.Search.childElementWithSelector(asset1RowEl, "[i9n-action-list]"), "[i9n-remove-asset]"));

            Test.Action.waitForModal();
        });
        /*
        it("they should see a confirmation modal", () => {
            Test.Verify.modalIsShown();
        });
        */
        describe("when user clicks No on the modal", () => {
            beforeAll(() => {
                Test.Action.clickModalNonPrimaryButton();
            });
            /*
            it("they should not see any errors on the page", () => {
                Test.Verify.noErrorOnPage();
            });

            it("they should get back to the manage data assets page and nothing should be removed", Test.asyncTest((doneUtil) => {
                Test.Verify.elementPresentWithTag("pcd-asset-groups-list");

                //  Verify asset list sections.
                Test.Verify.SharedComponent.verifyAssetListSectionFor({
                    teamNumber: 1,
                    assetNumber: 1
                }, doneUtil);
                Test.Verify.SharedComponent.verifyAssetListSectionFor({
                    teamNumber: 1,
                    assetNumber: 2
                }, doneUtil);
            }));
            */
        });
    });
});

describe("On manage data assets page for I9n_Team1", () => {
    let asset1RowEl: ElementFinder;

    beforeAll(() => {
        Test.Navigation.loadPage(`data-assets/manage/${generateFuzzyGuidFrom(1)}`, "manage-data-assets.remove-asset");
    });

    it("user should see remove link for I9n_Asset1_Team1 (2nd call)", () => {
        asset1RowEl = Test.Search.elementWithId(generateFuzzyGuidFrom(1));

        Test.Verify.childElementPresentWithSelector(
            Test.Search.childElementWithSelector(asset1RowEl, "[i9n-action-list]"), "[i9n-remove-asset]");
    });

    describe("when user clicks the remove link", () => {
        beforeAll(() => {
            //  Click the remove link.
            Test.Action.click(Test.Search.childElementWithSelector(
                Test.Search.childElementWithSelector(asset1RowEl, "[i9n-action-list]"), "[i9n-remove-asset]"));

            Test.Action.waitForModal();
        });

        it("they should see a confirmation modal (2nd call)", () => {
            Test.Verify.modalIsShown();
        });

        describe("when user confirms action on the modal", () => {
            beforeAll(() => {
                    Test.Action.clickCheckbox(Test.Search.childElementWithTag(
                        Test.Search.elementWithSelector("div[ui-view='modalContent']"), "mee-checkbox"));
                Test.Action.clickModalPrimaryButton();
            });
/*
            it("they should not see any errors on the page", () => {
                Test.Verify.noErrorOnPage();
            });
*/
/*
            it("they should get back to the manage data assets page and see I9n_Asset1_Team1 removed", Test.asyncTest((doneUtil) => {
                Test.Verify.elementPresentWithTag("pcd-asset-groups-list");

                //  Verify asset list sections.
                Test.Verify.elementAbsentWithId(generateFuzzyGuidFrom(1));
                Test.Verify.SharedComponent.verifyAssetListSectionFor({
                    teamNumber: 1,
                    assetNumber: 2
                }, doneUtil);
            }));
*/
        });
    });
});
