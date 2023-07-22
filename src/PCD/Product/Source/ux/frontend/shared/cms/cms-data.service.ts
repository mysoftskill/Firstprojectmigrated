import { Service, Inject } from "../../module/app.module";
import { ICmsApiService, ICmsDataService, CmsContentCollection, CmsKey, ICmsCache } from "./cms-types";
import { CmsUtilities } from "./cms-utilities";

@Service({
    name: "cmsDataService"
})
@Inject("cmsApiService", "cmsCache")
class CmsDataService implements ICmsDataService {

    constructor(
        private cmsApiService: ICmsApiService,
        private cmsCache: ICmsCache) {
    }

    public getContentItem<TCmsType>(cmsKey: CmsKey): TCmsType {

        return this.cmsCache.get(cmsKey);
    }

    public loadContentItems(cmsKeys: CmsKey[]): ng.IPromise<CmsContentCollection> {

        let contentKeysToLoad = _.filter(cmsKeys, (cmsKey) => !this.cmsCache.isInCache(cmsKey));

        return this.cmsApiService.getContentItems(contentKeysToLoad).then(cmsContents => {
            _.each(cmsContents.data, (cmsContent, key) => {
                this.cmsCache.set(key, cmsContent);
            });
            
            return _.object(
                cmsKeys.map(cmsKey => CmsUtilities.getCmsCompositeKey(cmsKey)),
                cmsKeys.map(cmsKey => this.cmsCache.get(cmsKey))
            );
        });
    }
}
