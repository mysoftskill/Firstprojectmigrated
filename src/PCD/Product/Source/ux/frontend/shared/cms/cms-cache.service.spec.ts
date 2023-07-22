import { TestSpec } from "../../shared-tests/spec.base";
import { CmsKey, ICmsCache, CmsAreaName } from "./cms-types";
import { CmsUtilities } from "./cms-utilities";

describe("Cms cache", () => {
    let cmsCache: ICmsCache;
    let cmsKey1: CmsKey, cmsKey2: CmsKey, cmsKey3: CmsKey, cmsKey4: CmsKey;
    let content1: any, content2: any, content3: any, content4;

    beforeEach(() => {
        cmsKey1 = {
            areaName: <CmsAreaName> "test-area1",
            cmsId: "page.test-content1"
        };
    
        cmsKey2 = {
            areaName: <CmsAreaName> "test-area2",
            cmsId: "page.test-content2"
        };
        
        cmsKey3 = {
            areaName: <CmsAreaName> "test-area3",
            cmsId: "page.test-content3"
        };
    
        cmsKey4= {
            areaName: <CmsAreaName> "test-area4",
            cmsId: "page.test-content4"
        };
    
        content1 = {
            strings: {
                string1: "string1"
            }
        };
    
        content2 = {
            strings: {
                string2: "string2"
            }
        };
        
        content3 = {
            strings: {
                string3: "string3"
            }
        };
    
        content4 = {
            strings: {
                string4: "string4"
            }
        };

        let spec = new TestSpec({
            preLoadedCmsContents: {
                [CmsUtilities.getCmsCompositeKey(cmsKey1)]: content1,
                [CmsUtilities.getCmsCompositeKey(cmsKey2)]: content2
            }
        });

        inject((_cmsCache_: ICmsCache) => {
            cmsCache = _cmsCache_;
        });
    });

    it("should add pre-loaded contents on ctor", () => {

        expect(cmsCache.get(cmsKey1)).toEqual(jasmine.objectContaining(content1));
        expect(cmsCache.get(cmsKey2)).toEqual(jasmine.objectContaining(content2));
    });

    it("set should store content in cache", () => {

        cmsCache.set(CmsUtilities.getCmsCompositeKey(cmsKey3), content3);
        expect(cmsCache.get(cmsKey3)).toEqual(jasmine.objectContaining(content3));
    });

    it("get should fail when content not in cache", () => {
        cmsCache.set(CmsUtilities.getCmsCompositeKey(cmsKey3), content3);
        expect(() => cmsCache.get(cmsKey4)).toThrowError();
    });
});
