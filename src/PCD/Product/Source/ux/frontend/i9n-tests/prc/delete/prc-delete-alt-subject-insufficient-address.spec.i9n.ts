import { Test } from "../../utilities/test";
import { generateFuzzyGuidFrom } from "../../../shared/guid";

describe("On manual requests Delete page", () => {
    beforeAll(() => {
        Test.Navigation.loadPage("manual-requests/delete", "manual-requests.delete.alt-subject.insufficient-address");
    });

    it("user should see the Delete request form elements (2nd call)", () => {
        Test.Verify.SharedComponent.verifyManualDeleteRequestForm();
    });

    describe("when user enters insufficient address information (no street name) for alt subject request and clicks Delete button", () => {
        beforeAll(() => {
            //  Note: this code assumes that alt-subject is selected by default, which is the current behavior.

            //  Enter CAP Id
            Test.Action.setText(Test.Search.childElementWithTag(
                Test.Search.elementWithSelector("mee-text-field[i9n-cap-id-field]"), "input"), "123456789");

            //  Enter Name
            Test.Action.setText(Test.Search.childElementWithTag(
                Test.Search.elementWithSelector("[i9n-name-selector]"), "input"), "I9n_Subject_Name");

            //  Enter Email
            Test.Action.setText(Test.Search.childElementWithTag(
                Test.Search.elementWithSelector("[i9n-email-selector]"), "input"), "I9n_Subject_Email");

            // Enter Unit number
            Test.Action.setText(Test.Search.childElementWithTag(
                Test.Search.elementWithSelector("[i9n-unit-selector]"), "input"), "I9n_Subject_Unit");

            //  Click checkbox and submit
            Test.Action.clickCheckbox(Test.Search.elementWithSelector("mee-checkbox[i9n-verify-info-checkbox]"));
            Test.Action.click(Test.Search.childElementWithTag(
                Test.Search.elementWithSelector("[i9n-delete-button]"), "button"));
        });

        it("user should see some error on the page", () => {
            Test.Verify.errorOnPage();
        });

        it("user should see an error for street name on the page", () => {
            Test.Verify.childElementContainsInputError(
                Test.Search.elementWithSelector("[i9n-street-name-selector]")
            );
        });

        describe("then user enters street name for alt subject request and clicks Delete button", () => {
            beforeAll(() => {
                // Enter Street name
                Test.Action.setText(Test.Search.childElementWithTag(
                    Test.Search.elementWithSelector("[i9n-street-name-selector]"), "input"), "I9n_Subject_Street");

                //  Click submit (since checkbox is checked)
                Test.Action.click(Test.Search.childElementWithTag(
                    Test.Search.elementWithSelector("[i9n-delete-button]"), "button"));
            });

            it("user should see some error on the page", () => {
                Test.Verify.errorOnPage();
            });

            it("user should see an error for city on the page", () => {
                Test.Verify.childElementContainsInputError(
                    Test.Search.elementWithSelector("[i9n-city-selector]")
                );
            });

            describe("then user enters city for alt subject request and clicks Delete button", () => {
                beforeAll(() => {
                    // Enter City
                    Test.Action.setText(Test.Search.childElementWithTag(
                        Test.Search.elementWithSelector("[i9n-city-selector]"), "input"), "I9n_Subject_City");

                    //  Click submit (since checkbox is checked)
                    Test.Action.click(Test.Search.childElementWithTag(
                        Test.Search.elementWithSelector("[i9n-delete-button]"), "button"));

                    Test.Action.waitForPageNavigation();
                });

                it("user should see the request completion page with relevant info", Test.asyncTest((doneUtil) => {
                    Test.Verify.SharedComponent.verifyPrcRequestConfirmationInfo("123456789", generateFuzzyGuidFrom(1), doneUtil);
                }));

                it("user should see links for additional actions", Test.asyncTest((doneUtil) => {
                    Test.Verify.SharedComponent.verifyPrcRequestConfirmationLinks(doneUtil);
                }));
            });
        });
    });
});
