import { Test } from "../../utilities/test";
import { generateFuzzyGuidFrom } from "../../../shared/guid";
import { PrivacySubjectDetailTypeId } from "../../../shared/manual-requests/manual-request-types";

describe("On manual requests Export page", () => {
    beforeAll(() => {
        Test.Navigation.loadPage("manual-requests/export", "manual-requests.export.msa");
    });

    it("user should see the Export request form elements (4th call)", () => {
        Test.Verify.SharedComponent.verifyManualExportRequestForm();
    });

    describe("when user enters information for MSA request and clicks Export button", () => {
        beforeAll(Test.asyncTest((doneUtil) => {
            //  Select MSA from subject selector dowp down
            Test.Action.clickMeeSelectOptionFor("[i9n-subject-selector]", PrivacySubjectDetailTypeId.MSA, doneUtil);
            
            //  Enter CAP Id
            Test.Action.setText(Test.Search.childElementWithTag(
                Test.Search.elementWithSelector("[i9n-cap-id-field]"), "input"), "123456789");

            //  Enter proxy ticket
            Test.Action.setText(Test.Search.childElementWithTag(
                Test.Search.elementWithTag("pcd-privacy-subject-msa-identifier"), "textarea"), "I9n_Proxy_Ticket");

            //  Click Export
            Test.Action.click(Test.Search.childElementWithTag(
                Test.Search.elementWithSelector("[i9n-export-button]"), "button"));

            Test.Action.waitForPageNavigation();
        }));

        it("user should not see any errors on the page", () => {
            Test.Verify.noErrorOnPage();
        });

        it("user should see the request completion page with relevant info", Test.asyncTest((doneUtil) => {
            Test.Verify.SharedComponent.verifyPrcRequestConfirmationInfo("123456789", generateFuzzyGuidFrom(1), doneUtil);
        }));

        it("user should see links for additional actions", Test.asyncTest((doneUtil) => {
            Test.Verify.SharedComponent.verifyPrcRequestConfirmationLinks(doneUtil);
        }));
    });
});
