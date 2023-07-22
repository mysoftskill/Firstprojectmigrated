import { element, by, ElementFinder } from "protractor";

export class Search {

    //  Finds an element with the specified Id.
    public static elementWithId(id: string): ElementFinder {
        return element(by.id(id));
    }

    //  Finds an element with the specified Tag.
    public static elementWithTag(tagName: string): ElementFinder {
        return element(by.tagName(tagName));
    }

    //  Finds an element with the specified CSS selector. Also used for searching by attributes.
    public static elementWithSelector(selector: string): ElementFinder {
        return element(by.css(selector));
    }

    //  Finds an element with the specified CSS class.
    public static elementWithClass(cssClass: string): ElementFinder {
        return element(by.className(cssClass));
    }

    //  Finds a child element with the specified Id.
    public static childElementWithId(parentEl: ElementFinder, id: string): ElementFinder {
        return parentEl.element(by.id(id));
    }

    //  Finds a child element with the specified Tag.
    public static childElementWithTag(parentEl: ElementFinder, tagName: string): ElementFinder {
        return parentEl.element(by.tagName(tagName));
    }

    //  Finds a child element with the specified CSS selector. Also used for searching children by attributes.
    public static childElementWithSelector(parentEl: ElementFinder, selector: string): ElementFinder {
        return parentEl.element(by.css(selector));
    }

    //  Finds a child element with the specified CSS class.
    public static childElementWithClass(parentEl: ElementFinder, cssClass: string): ElementFinder {
        return parentEl.element(by.className(cssClass));
    }
}
