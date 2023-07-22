import { Test } from "../utilities/test";
import { generateFuzzyGuidFrom } from "../../shared/guid";

describe("On manage data assets page for I9n_Team1", () => {
    beforeAll(() => {
        Test.Navigation.loadPage(`data-assets/manage/${generateFuzzyGuidFrom(1)}`, "manage-data-assets");
    });
    /*
    it("user should see team related information in page context drawer", Test.asyncTest((doneUtil) => {
        Test.Verify.SharedComponent.verifyOwnerContextDrawerForTeamNumber(1, doneUtil);
    }));
    */
    /*
    it("user should see asset group list containing I9n_Team1 assets", Test.asyncTest((doneUtil) => {
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
    /*
    it("user should see additional links to register/move data assets", Test.asyncTest((doneUtil) => {
        Test.Verify.elementPresentWithSelector("[i9n-register-data-asset]");
        Test.Verify.linkPointsToLocation(
            Test.Search.elementWithSelector("[i9n-register-data-asset]"),
            `data-assets/create/${generateFuzzyGuidFrom(1)}`,
            doneUtil.addPromiseToDone()
        );

        Test.Verify.elementPresentWithSelector("[i9n-move-data-assets]");
        //  This link opens up the email template, so not verifying anything here.
    }));
    */
    /*
    it("user should see disabled buttons for linking/unlinking agents and variants", () => {
        let linkAgentSelector = "[i9n-link-data-agent]";
        Test.Verify.elementPresentWithSelector(linkAgentSelector);
        Test.Verify.elementIsDisabled(Test.Search.elementWithSelector(linkAgentSelector));

        let unlinkAgentSelector = "[i9n-unlink-data-agent]";
        Test.Verify.elementPresentWithSelector(unlinkAgentSelector);
        Test.Verify.elementIsDisabled(Test.Search.elementWithSelector(unlinkAgentSelector));

        let linkVariantsSelector = "[i9n-link-variants]";
        Test.Verify.elementPresentWithSelector(linkVariantsSelector);
        Test.Verify.elementIsDisabled(Test.Search.elementWithSelector(linkVariantsSelector));
    });
    */
    it("user should not see any errors on the page", () => {
        Test.Verify.noErrorOnPage();
    });
});
