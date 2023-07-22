import { ElementFinder, browser, ExpectedConditions, by, element } from "protractor";
import { Search } from "./search";
import { SelectorTypeClassName } from "../../components/shared/directory-resource-selector/directory-resource-selector-types";
import { DoneUtil } from "./done-util";

export class Action {

    //  Clicks an element.
    public static click(el: ElementFinder): void {
        el.click();
    }

    //  Simulates user input on the text field.
    public static setText(el: ElementFinder, text: string): void {
        el.clear();
        el.sendKeys(text);
    }

    //  Waits for element to appear in DOM before proceeding.
    public static waitForElement(el: ElementFinder, waitTimeoutInMillis: number, text: string): void {
        browser.wait(ExpectedConditions.presenceOf(el), waitTimeoutInMillis, text);
    }

    //  Waits for element to disappear from the DOM before proceeding.
    public static waitForElementAbsence(el: ElementFinder, waitTimeoutInMillis: number, text: string): void {
        browser.wait(ExpectedConditions.invisibilityOf(el), waitTimeoutInMillis, text);
    }

    //  Waits for initial page to load before proceeding.
    public static waitForPageLoad(): void {
        Action.waitForElement(Search.elementWithTag("pcd-heading"), 5000, "Page is taking too long to appear in the DOM");
    }

    //  Waits for pcd-team-picker appears
    public static waitForTeamPicker(): void {
        Action.waitForElement(Search.elementWithTag("pcd-team-picker"), 5000, "Page is taking too long to appear in the DOM");
    }

    //  Waits for navigated page to load before proceeding.
    public static waitForPageNavigation(): void {
        this.waitForPageLoad();
    }

    //  Waits for modal to appear before proceeding.
    public static waitForModal(): void {
        Action.waitForElement(element(by.css("div[ui-view='modalContent']")), 5000, "Modal is taking too long to appear");
    }

    //  Waits for modal to dismiss before proceeding.
    public static waitForModalDismiss(): void {
        Action.waitForElementAbsence(element(by.css("div[ui-view='modalContent']")), 5000, "Modal is taking too long to dismiss");
    }

    /** Enters text on the DRS instance of specified class,
     * then clicks the first option in dropdown. */
    public static drsSetTextAndClickFirstOption(selectorClass: SelectorTypeClassName, text: string): void {
        //  Enter text in DRS selector.
        let drsInput = Search.childElementWithTag(Search.elementWithClass(selectorClass), "input");
        Action.setText(drsInput, text);

        //  Click first option.
        Action.clickFirstDrsOption(selectorClass);
    }

    //  Clicks the first option in DRS dropdown.
    public static clickFirstDrsOption(selectorClass: string): void {
        let drsDropDownSpan = Search.childElementWithTag(Search.childElementWithSelector(
            Search.elementWithClass(selectorClass), "div[mee-text-action-suggestion-box]"), "span");

        Action.click(drsDropDownSpan);
    }

    /** Searches for the mee-select element based on `meeSelectElementSelector`, then clicks the option
     * whose value matches `optionValue`. */
    public static clickMeeSelectOptionFor(meeSelectElementSelector: string, optionValue: string, doneUtil: DoneUtil): void {
        let meeSelectElement = Search.elementWithSelector(meeSelectElementSelector);
        Action.click(Search.childElementWithTag(meeSelectElement, "button"));

        let optionEl = Search.childElementWithSelector(meeSelectElement, `[value="${optionValue}"]`);
        let donePromise = doneUtil.addPromiseToDone();
        optionEl.getAttribute("data-value")
            .then((attrValue: string) => {
                Action.click(Search.childElementWithId(meeSelectElement, attrValue));
                donePromise.fulfill();
            })
            .catch(() => donePromise.reject());
    }

    //  Clicks the primary button on the modal.
    public static clickModalPrimaryButton(): void {
        Action.click(Search.childElementWithSelector(
            Search.elementWithTag("pcd-confirmation-modal-actions"), "button[mee-button='primary']"));

        Action.waitForModalDismiss();
    }

    //  Clicks the non-primary button on the modal.
    public static clickModalNonPrimaryButton(): void {
        Action.click(Search.childElementWithSelector(
            Search.elementWithTag("pcd-confirmation-modal-actions"), "button[mee-button]:not([mee-button='primary'])"));
        Action.waitForModalDismiss();
    }

    //  Clicks the specified checkbox.
    public static clickCheckbox(el: ElementFinder): void {
        Action.click(Search.childElementWithTag(el, "span"));
    }
}
