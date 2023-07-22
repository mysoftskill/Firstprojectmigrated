import { Config, Inject } from "../../module/app.module";

import { IMocksService } from "../mocks.service";
import { ICmsDataService, CmsKey, CmsContentCollection, CmsPageContent, CmsComponentContent } from "./cms-types";
import { CmsUtilities } from "./cms-utilities";

/**
 * Mocks CMS data service.
 */
class CmsDataMockService implements ICmsDataService {
    @Config()
    @Inject("$provide")
    public static configurePdmsDataMockService($provide: ng.auto.IProvideService): void {
        //  Decorate AJAX service with a function that will add authentication header to each outgoing request.
        $provide.decorator("cmsDataService", ["$delegate", "$q", "mocksService",
            ($delegate: ICmsDataService,
                $q: ng.IQService,
                mocksService: IMocksService,
            ): ICmsDataService => {
                return mocksService.isActive() ? new CmsDataMockService(
                    $delegate,
                    $q,
                    mocksService,
                ) : $delegate;
            }
        ]);
    }

    constructor(
        private readonly real: ICmsDataService,
        private readonly $promises: ng.IQService,
        private readonly mocksService: IMocksService,
    ) {
        console.debug("Using mocked Cms service.");
    }

    public getContentItem<TCmsType>(cmsKey: CmsKey): TCmsType {
        let content = this.real.getContentItem<TCmsType>(cmsKey);
        this.mockContent_s(content);

        return content;
    }

    public loadContentItems(cmsKeys: CmsKey[]): ng.IPromise<CmsContentCollection> {
        return this.real.loadContentItems(cmsKeys)
            .then((cmsCollection: CmsContentCollection) => {
                _.forEach(cmsCollection, (cmsContent) => {
                    this.mockContent_s(cmsContent);
                });

                return cmsCollection;
            });
    }

    private mockContent_s(cmsContent: any): void {
        cmsContent._s = mocked_s;
    }

}

function mocked_s(key: string): string {
    return key;
}
