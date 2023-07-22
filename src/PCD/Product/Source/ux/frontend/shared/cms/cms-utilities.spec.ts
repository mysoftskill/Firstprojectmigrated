import { CmsAreaName, RichParagraph } from "./cms-types";
import { getCmsString, CmsUtilities, getCmsParagraph } from "./cms-utilities";

describe("Cms utilities ", () => {

    it("can create composite key", () => {
        // arrange
        let cmsKey = { areaName: <CmsAreaName> "test-area", cmsId: "test-content1" };

        //act
        let compositeKey = CmsUtilities.getCmsCompositeKey(cmsKey);

        // act
        expect(compositeKey).toBe("TEST-CONTENT1@TEST-AREA");
    });

    describe("getCmsString", () => {

        it("should return value when content has strings dictionary ", () => {
            // arrange 
            let content = { 
                strings: {
                    prop1: "sample string 1",
                    prop2: "sample string 2"
                }
            };

            // act/assert
            expect(getCmsString(content, "prop1")).toBe(content.strings.prop1);
            expect(getCmsString(content, "prop2")).toBe(content.strings.prop2);
        });

        it("should return key name when key missing in strings dictionary ", () => {
            // arrange 
            let content = { 
                strings: {
                }
            };

            // act/assert
            expect(getCmsString(content, "prop3")).toBe(`ERROR: prop3`);
            expect(getCmsString(content, "prop4")).toBe(`ERROR: prop4`);
        });

        it("should throw exception when content does not contain strings dictionary ", () => {
            // arrange 
            let content = { 
            };

            // act/assert
            expect(() => getCmsString(content, "prop1")).toThrowError();
        });
    });

    describe("getCmsParagraph", () => {
        it("should return value when content has paragraphs dictionary ", () => {
            // arrange 
            let content = {
                paragraphs: {
                    prop1: <RichParagraph> {
                        text: "sample string 1",
                        cssClassName: "class1",
                        style: []
                    },
                    prop2: <RichParagraph> {
                        text: "sample string 2",
                        cssClassName: "class2",
                        style: []
                    }
                }
            };

            // act/assert
            expect(getCmsParagraph(content, "prop1")).toBe(content.paragraphs.prop1);
            expect(getCmsParagraph(content, "prop2")).toBe(content.paragraphs.prop2);
        });

        it("should return key name when key missing in paragraphs dictionary ", () => {
            // arrange 
            let content = {
                paragraphs: {
                }
            };

            // act/assert
            expect(getCmsParagraph(content, "prop3").text).toEqual(`ERROR: prop3`);
        });

        it("should throw exception when content does not contain paragraphs dictionary ", () => {
            // arrange 
            let content = {
            };

            // act/assert
            expect(() => getCmsParagraph(content, "prop1")).toThrowError();
        });
    });
});
