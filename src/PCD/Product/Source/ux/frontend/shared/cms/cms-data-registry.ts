import { CmsAreaName, CmsKey } from "./cms-types";

type AreaRegistry = {
    [key: string]: string[];
};

class CmsDataRegistry {

    private cmsContents: AreaRegistry = {};
    private static instance: CmsDataRegistry;

    // do not remove empty private constructor which prevents instantiation, only singleton instance of CmsDataRegistry can accessed using getInstance().
    private constructor() {
    }

    public static getInstance() {
        return this.instance || (this.instance = new CmsDataRegistry());
    }

    public registerContent(cmsKey: CmsKey) {
        if(!this.cmsContents[cmsKey.areaName]) {
            this.cmsContents[cmsKey.areaName] = [];
        }

        let areaContents = this.cmsContents[cmsKey.areaName];
        
        if(areaContents.indexOf(cmsKey.cmsId) === -1) {
            areaContents.push(cmsKey.cmsId);
        }
    }

    public getCmsByAreas(areaNames: CmsAreaName[]): CmsKey[] {
        return _.flatten(
            areaNames.map(areaName => 
                _.map(this.cmsContents[areaName], key => { 
                    return <CmsKey> { areaName: areaName,  cmsId: key };
                }) 
            ));
    }
}

export const CmsDataRegistryInstance = CmsDataRegistry.getInstance();