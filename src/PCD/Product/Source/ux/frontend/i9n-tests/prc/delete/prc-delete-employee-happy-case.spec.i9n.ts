import { Test } from "../../utilities/test";
import { generateFuzzyGuidFrom } from "../../../shared/guid";
import { PrivacySubjectDetailTypeId } from "../../../shared/manual-requests/manual-request-types";

describe("On manual requests Delete page", () => {
    beforeAll(() => {
        Test.Navigation.loadPage("manual-requests/delete", "manual-requests.delete.employee");
    });

    it("user should see the Delete request form elements (3rd call)", () => {
        Test.Verify.SharedComponent.verifyManualDeleteRequestForm();
    });

    describe("when user enters information for Microsoft employee and clicks Delete button", () => {
        beforeAll(Test.asyncTest((doneUtil) => {
            //  Select Microsoft employee from subject selector dowp down
            Test.Action.clickMeeSelectOptionFor("[i9n-subject-selector]", PrivacySubjectDetailTypeId.MicrosoftEmployee, doneUtil);

            //  Enter CAP Id
            Test.Action.setText(Test.Search.childElementWithTag(
                Test.Search.elementWithSelector("[i9n-cap-id-field]"), "input"), "123456789");

            //  Enter email
            Test.Action.setText(Test.Search.childElementWithTag(
                Test.Search.elementWithSelector("[i9n-email-selector]"), "input"), "I9n_Subject_Email");

            //  Enter employee number
            Test.Action.setText(Test.Search.childElementWithTag(
                Test.Search.elementWithSelector("[i9n-employee-number]"), "input"), "I9n_Employee_Number");

            //  Enter employee start date
            Test.Action.setText(Test.Search.childElementWithTag(
                Test.Search.elementWithSelector("[i9n-start-date]"), "input"), "01-01-2018");

            //  Click checkbox and submit
            Test.Action.clickCheckbox(Test.Search.elementWithSelector("[i9n-verify-info-checkbox]"));
            Test.Action.click(Test.Search.childElementWithTag(
                Test.Search.elementWithSelector("pcd-commit-request-button[i9n-delete-button]"), "button"));

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
