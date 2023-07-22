import { CmsAreaName, CmsKey } from "./cms-types";
import { getCmsString, CmsUtilities } from "./cms-utilities";
import { CmsDataRegistryInstance } from "./cms-data-registry";

describe("CMS data registry ", () => {

    let cmsDataRegistry = CmsDataRegistryInstance;

    it("can get CmsKeys by areas", () => {

        // arrange
        let cmsKey1 = {
            areaName: <CmsAreaName>"test-area1",
            cmsId: "page.test-content1"
        };

        let cmsKey2 = {
            areaName: <CmsAreaName> "test-area1",
            cmsId: "page.test-content2"
        };

        let cmsKey3 = {
            areaName: <CmsAreaName> "test-area2",
            cmsId: "page.test-content3"
        };

        let cmsKey4 = {
            areaName: <CmsAreaName> "test-area3",
            cmsId: "page.test-content4"
        };

        // act
        cmsDataRegistry.registerContent(cmsKey1);
        cmsDataRegistry.registerContent(cmsKey2);
        cmsDataRegistry.registerContent(cmsKey3);
        cmsDataRegistry.registerContent(cmsKey4);

        //assert
        let cmsKeys = cmsDataRegistry.getCmsByAreas([<CmsAreaName>"test-area1", <CmsAreaName>"test-area2"]);
        expect(cmsKeys.length).toBe(3);
        expect(cmsKeys.filter(k => k.cmsId === cmsKey1.cmsId && k.areaName === cmsKey1.areaName).length).toBe(1);
        expect(cmsKeys.filter(k => k.cmsId === cmsKey2.cmsId && k.areaName === cmsKey2.areaName).length).toBe(1);
        expect(cmsKeys.filter(k => k.cmsId === cmsKey3.cmsId && k.areaName === cmsKey3.areaName).length).toBe(1);
        

        cmsKeys = cmsDataRegistry.getCmsByAreas([<CmsAreaName> "test-area3"]);
        expect(cmsKeys.length).toBe(1);
        expect(cmsKeys.filter(k => k.cmsId === cmsKey4.cmsId && k.areaName === cmsKey4.areaName).length).toBe(1);
    });

});
