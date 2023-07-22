import { browser, element, by, ElementFinder, promise } from "protractor";
import { Search } from "../search";
import { VerifySharedComponent } from "./verify-shared-component";

export class Verify {
    public static SharedComponent = VerifySharedComponent;

    //  Verifies that an element is present with the specified Id.
    public static elementPresentWithId(id: string): void {
        expect(Search.elementWithId(id).isPresent()).toBeTruthy();
    }

    //  Verifies that an element is absent with the specified Id.
    public static elementAbsentWithId(id: string): void {
        expect(Search.elementWithId(id).isPresent()).toBeFalsy();
    }

    //  Verifies that an element is present with the specified CSS class.
    public static elementPresentWithClass(cssClass: string): void {
        expect(Search.elementWithClass(cssClass).isPresent()).toBeTruthy();
    }

    //  Verifies that an element is present with the specified tag.
    public static elementPresentWithTag(tagName: string): void {
        expect(Search.elementWithTag(tagName).isPresent()).toBeTruthy();
    }

    //  Verifies that an element is present with the specified selector.
    public static elementPresentWithSelector(selector: string): void {
        expect(Search.elementWithSelector(selector).isPresent()).toBeTruthy();
    }

    //  Verifies that a child element is present with the specified Id.
    public static childElementPresentWithId(parentEl: ElementFinder, id: string): void {
        expect(Search.childElementWithId(parentEl, id).isPresent()).toBeTruthy();
    }

    //  Verifies that a child element is present with the specified CSS class.
    public static childElementPresentWithClass(parentEl: ElementFinder, cssClass: string): void {
        expect(Search.childElementWithClass(parentEl, cssClass).isPresent()).toBeTruthy();
    }

    //  Verifies that a child element is present with the specified tag.
    public static childElementPresentWithTag(parentEl: ElementFinder, tagName: string): void {
        expect(Search.childElementWithTag(parentEl, tagName).isPresent()).toBeTruthy();
    }

    //  Verifies that a child element is present with the specified selector.
    public static childElementPresentWithSelector(parentEl: ElementFinder, selector: string): void {
        expect(Search.childElementWithSelector(parentEl, selector).isPresent()).toBeTruthy();
    }

    //  Verifies that an element contains specified text.
    public static inputContainsText(el: ElementFinder, searchText: string, donePromise: promise.Deferred<{}>): void {
        el.getAttribute("value")
            .then(text => {
                expect(text.indexOf(searchText)).toBeGreaterThan(-1);
                donePromise && donePromise.fulfill();
            })
            .catch(() => donePromise && donePromise.reject());
    }

    //  Verifies that a child element contains an input error.
    public static childElementContainsInputError(parentEl: ElementFinder): void {
        Verify.childElementPresentWithSelector(parentEl, "input.error-input");
    }

    //  Verifies that an element contains specified text.
    public static elementContainsText(el: ElementFinder, searchText: string, donePromise: promise.Deferred<{}>): void {
        el.getText()
            .then(text => {
                expect(text.indexOf(searchText)).toBeGreaterThan(-1);
                donePromise && donePromise.fulfill();
            })
            .catch(() => donePromise && donePromise.reject());
    }

    //  Verifies that an element does not contain specified text.
    public static elementDoesNotContainText(el: ElementFinder, searchText: string, donePromise: promise.Deferred<{}>): void {
        el.getText()
            .then(text => {
                expect(text.indexOf(searchText)).toEqual(-1);
                donePromise && donePromise.fulfill();
            })
            .catch(() => donePromise && donePromise.reject());
    }

    //  Verifies that an element is disabled.
    public static elementIsDisabled(el: ElementFinder): void {
        expect(el.isEnabled()).toBeFalsy();
    }

    //  Verifies that href attribute of the element points to a specified url path.
    public static linkPointsToLocation(el: ElementFinder, urlPath: string, donePromise: promise.Deferred<{}>): void {
        el.getAttribute("href")
            .then(attrVal => {
                if (!attrVal) {
                    console.error("Please pass an anchor tag with href attribute.");
                    donePromise.reject();
                } else {
                    expect(attrVal).toBe(`${browser.baseUrl}${urlPath}`);
                    donePromise.fulfill();
                }
            })
            .catch(() => donePromise.reject());
    }

    //  Verifies an error exists on page.
    public static errorOnPage(): void {
        Verify.elementPresentWithSelector("mee-inline-error :first-child");
    }

    //  Verifies no error on page.
    public static noErrorOnPage(): void {
        expect(Search.elementWithSelector("mee-inline-error :first-child").isPresent()).toBeFalsy();
    }

    //  Verifies that a modal is shown on page.
    public static modalIsShown(): void {
        expect(element(by.css("div[role='dialog']")).isPresent()).toBeTruthy();
    }
}
