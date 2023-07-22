import { Service, Inject } from "../../module/app.module";
import { CmsKey, ICmsCache, CmsContentCollection } from "./cms-types";
import { getWrappedCmsContent, CmsUtilities } from "./cms-utilities";

type InternalCache = {
    [key: string]: any;
};

@Service({
    name: "cmsCache"
})
@Inject("preLoadedCmsContentItems")
class CmsCache implements ICmsCache {
    private cmsCache: InternalCache = {};

    public constructor(
        private preLoadedCmsContentItems: CmsContentCollection) {
        
        _.forEach(this.preLoadedCmsContentItems, (content, key) => {
            this.set(key, content);
        });
    }

    public get(cmsKey: CmsKey): any {
        let cacheKey = CmsUtilities.getCmsCompositeKey(cmsKey);
        let content = this.cmsCache[cacheKey];

        if(!content) {
            throw new Error(`Cms item ${cacheKey} was never loaded, use @Route decorator to load required Cms areas`);
        }

        return getWrappedCmsContent(content);
    }

    public set(cmsCompositeKey: string, content: any): void {
        this.cmsCache[cmsCompositeKey] = content;
    }

    public isInCache(cmsKey: CmsKey): boolean {
        return !!this.cmsCache[CmsUtilities.getCmsCompositeKey(cmsKey)];
    }

}