import * as angular from "angular";
import { TestSpec, SpyCache } from "../../shared-tests/spec.base";
import { ICmsDataService, ICmsApiService, ICmsCache, CmsContentCollection, CmsKey, CmsAreaName } from "./cms-types";

describe("Cms data service", () => {
    let spec: TestSpec;
    let cmsDataService: ICmsDataService;
    let cmsCacheMock: SpyCache<ICmsCache>;
    let cmsApiServiceMock: SpyCache<ICmsApiService>;

    beforeEach(() => {
        spec = new TestSpec();

        inject((_cmsApiService_: ICmsApiService, _cmsDataService_: ICmsDataService, _cmsCache_: ICmsCache) => {
            cmsApiServiceMock = new SpyCache(_cmsApiService_);
            cmsDataService = _cmsDataService_;
            cmsCacheMock = new SpyCache(_cmsCache_);
        });
    });

    it("loads content items, utilizes cache and caches newly fetched contents", () => {
        // arrange
        let cmsKeyInCache = { 
            areaName: <CmsAreaName> "test-area", 
            cmsId: "test-content1" 
        };
        
        let cmsKeyNotInCache = { 
            areaName: <CmsAreaName> "test-area", 
            cmsId: "test-content2" 
        };

        let newlyLoadedCmsContent = { 
            strings: {
                string1: "cms string1"
            }
        };
        
        let newlyLoadedCmsContentCollection: CmsContentCollection = {
            [`${cmsKeyNotInCache.cmsId}@${cmsKeyNotInCache.areaName}`]: newlyLoadedCmsContent
        };

        let cachedCmsContent = { 
            strings: {
                string1: "cms string2"
            }
        };

        cmsApiServiceMock.getFor("getContentItems").and.returnValue(spec.asHttpPromise(newlyLoadedCmsContentCollection));
        cmsCacheMock.getFor("get").and.returnValue(cachedCmsContent);
        cmsCacheMock.getFor("set").and.stub();
        // mock isInCache to return true for cmsKeys[0] and false otherwise
        cmsCacheMock.getFor("isInCache").and.callFake((key: CmsKey) => key === cmsKeyInCache);

        // act
        cmsDataService.loadContentItems([cmsKeyInCache, cmsKeyNotInCache]);
        spec.runDigestCycle();

        // assert
        expect(cmsApiServiceMock.getFor("getContentItems")).toHaveBeenCalledWith([cmsKeyNotInCache]);
        
        expect(cmsCacheMock.getFor("isInCache")).toHaveBeenCalledTimes(2);
        
        expect(cmsCacheMock.getFor("set")).toHaveBeenCalledWith(`${cmsKeyNotInCache.cmsId}@${cmsKeyNotInCache.areaName}`, newlyLoadedCmsContent);
        expect(cmsCacheMock.getFor("set")).not.toHaveBeenCalledWith(`${cmsKeyInCache.cmsId}@${cmsKeyInCache.areaName}`, jasmine.any(Object));
    });
});
