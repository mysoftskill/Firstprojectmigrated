import * as angular from "angular";
import { TestSpec, SpyCache } from "../shared-tests/spec.base";

import { StringUtilities, PropertyBag } from "./string-utilities";

describe("StringUtilities", () => {
    let spec: TestSpec;
    let displayStringsByKey: PropertyBag;

    beforeEach(() => {
        spec = new TestSpec();

        displayStringsByKey = {
            textToType: "Text to type",
            readingText: "Read this text"
        };
    });

    describe("getCommaSeparatedList", () => {
        it("returns supplied default value for an empty input", () => {
            expect(StringUtilities.getCommaSeparatedList([], displayStringsByKey, "None")).toBe("None");
        });

        it("returns the comma separated dictionary output for an input with known keys", () => {
            expect(StringUtilities.getCommaSeparatedList(["textToType", "readingText"], displayStringsByKey, "None"))
                .toBe("Text to type, Read this text");
        });

        it("returns the only item in the list without a trailing comma", () => {
            expect(StringUtilities.getCommaSeparatedList(["textToType"], displayStringsByKey, "None"))
                .toBe("Text to type");
        });

        it("returns the key if not found in the dictionary", () => {
            expect(StringUtilities.getCommaSeparatedList(["textToType", "unknown1", "readingText"], displayStringsByKey, "None"))
                .toBe("Text to type, unknown1, Read this text");
        });
    });

    describe("queryStringOf", () => {
        it("returns correct output for simple key value pairs", () => {
            expect(StringUtilities.queryStringOf({
                key1: "value1",
                key2: "value2"
            })).toBe("key1=value1&key2=value2");
        });

        it("returns correct output when key value pairs include array", () => {
            expect(StringUtilities.queryStringOf({
                key1: "xyz",
                key2: ["abc", "def"]
            })).toBe("key1=xyz&key2=abc%2Cdef");
        });
    });

    describe("areEqual", () => {
        it("returns true when both strings are equal, ignores case", () => {
            expect(StringUtilities.areEqualIgnoreCase("string", "string")).toBe(true);
            expect(StringUtilities.areEqualIgnoreCase("STRING", "STRING")).toBe(true);
            expect(StringUtilities.areEqualIgnoreCase("StrinG", "sTRIng")).toBe(true);
        });

        it("returns false when both strings are not equal, ignores case", () => {
            expect(StringUtilities.areEqualIgnoreCase("one string", "another string")).toBe(false);
        });

        it("falls back to === when both strings are falsy", () => {
            expect(StringUtilities.areEqualIgnoreCase("", "")).toBe("" === "");
            expect(StringUtilities.areEqualIgnoreCase(null, null)).toBe(null === null);
            expect(StringUtilities.areEqualIgnoreCase(undefined, undefined)).toBe(undefined === undefined);
            expect(StringUtilities.areEqualIgnoreCase("", null)).toBe("" === null);
            expect(StringUtilities.areEqualIgnoreCase(undefined, null)).toBe(undefined === null);
            expect(StringUtilities.areEqualIgnoreCase("", undefined)).toBe("" === undefined);
        });

        it("falls back to === when one of the strings is falsy", () => {
            expect(StringUtilities.areEqualIgnoreCase("", "string")).toBe(false);   //  NOTE: can't use TypeScript expression for that.
            expect(StringUtilities.areEqualIgnoreCase("string", null)).toBe("string" === null);
            expect(StringUtilities.areEqualIgnoreCase(undefined, "string")).toBe(undefined === "string");
        });
    });

    describe("contains", () => {
        it("returns true when one string contains another one, ignores case", () => {
            expect(StringUtilities.containsIgnoreCase("the quick brown fox jumps over the lazy dog", "BROWN FOX")).toBe(true);
            expect(StringUtilities.containsIgnoreCase("THE QUICK BROWN FOX JUMPS OVER THE LAZY DOG", "Brown Fox")).toBe(true);
            expect(StringUtilities.containsIgnoreCase("The Quick Brown Fox Jumps Over The Lazy Dog", "brown fox")).toBe(true);
            expect(StringUtilities.containsIgnoreCase("", "")).toBe(true);
        });

        it("returns false when one string doesn't contain another one", () => {
            expect(StringUtilities.containsIgnoreCase("the quick brown fox jumps over the lazy dog", "Lorem Ipsum")).toBe(false);
        });

        it("returns false when one of the strings is falsy", () => {
            expect(StringUtilities.containsIgnoreCase("", "the quick brown fox jumps over the lazy dog")).toBe(false);
            expect(StringUtilities.containsIgnoreCase("the quick brown fox jumps over the lazy dog", null)).toBe(false);
            expect(StringUtilities.containsIgnoreCase(undefined, "the quick brown fox jumps over the lazy dog")).toBe(false);
            expect(StringUtilities.containsIgnoreCase(null, null)).toBe(false);
            expect(StringUtilities.containsIgnoreCase(undefined, undefined)).toBe(false);
        });
    });
});
