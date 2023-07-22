import { Test } from "../../utilities/test";
import { generateFuzzyGuidFrom } from "../../../shared/guid";

describe("On manual requests Delete page", () => {
    beforeAll(() => {
        Test.Navigation.loadPage("manual-requests/delete", "manual-requests.delete.alt-subject");
    });

    it("user should see the Delete request form elements", () => {
        Test.Verify.SharedComponent.verifyManualDeleteRequestForm();
    });

    describe("when user enters information for alt subject request and clicks Delete button", () => {
        beforeAll(() => {
            //  Note: this code assumes that alt-subject is selected by default, which is the current behavior.

            //  Enter CAP Id
            Test.Action.setText(Test.Search.childElementWithTag(
                Test.Search.elementWithSelector("mee-text-field[i9n-cap-id-field]"), "input"), "123456789");

            //  Enter Name
            Test.Action.setText(Test.Search.childElementWithTag(
                Test.Search.elementWithSelector("pcd-directory-resource-selector[i9n-name-selector]"), "input"), "I9n_Subject_Name");

            //  Enter Email
            Test.Action.setText(Test.Search.childElementWithTag(
                Test.Search.elementWithSelector("pcd-directory-resource-selector[i9n-email-selector]"), "input"), "I9n_Subject_Email");

            //  Click checkbox and submit
            Test.Action.clickCheckbox(Test.Search.elementWithSelector("mee-checkbox[i9n-verify-info-checkbox]"));
            Test.Action.click(Test.Search.childElementWithTag(
                Test.Search.elementWithSelector("pcd-commit-request-button[i9n-delete-button]"), "button"));

            Test.Action.waitForPageNavigation();
        });

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
