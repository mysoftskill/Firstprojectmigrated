import { SpyCache } from "./spy-cache";
import * as Pdms from "../shared/pdms/pdms-types";
import { IGraphDataService } from "../shared/graph/graph-data.service";
import { IManualRequestsDataService } from "../shared/manual-requests/manual-requests-data.service";
import { IVariantDataService } from "../shared/variant/variant-data.service";
import { ICmsDataService } from "../shared/cms/cms-types";
import { IGroundControlDataService } from "../shared/flighting/ground-control-data.service";
import { extend } from "webdriver-js-extender";
import { IGroundControlApiService } from "../shared/flighting/ground-control-api.service";

export type MethodOf<TDataService> = keyof TDataService;

//  Generic mock of a data service.
export abstract class DataServiceMock<TDataService> {
    //  Cache of Jasmine spies for service.
    private readonly spiesImpl: SpyCache<TDataService>;

    protected constructor(
        private readonly dataService: TDataService,
        private readonly $promises: ng.IQService) {

        this.spiesImpl = new SpyCache<TDataService>(this.dataService);
    }

    //  Gets data service instance.
    public get instance(): TDataService {
        return this.dataService;
    }

    //  Gets access to service spies.
    public get spies(): SpyCache<TDataService> {
        return this.spiesImpl;
    }

    /**
     * Gets the spy for 'method'.
     * @param method
     */
    public getFor(method: MethodOf<TDataService>): jasmine.Spy {
        return this.spiesImpl.getFor(method);
    }

    /**
     * Mocks async result of a data service method.
     * @param method Method name.
     * @param value Optional value to be returned as part of resolved promise.
     */
    public mockAsyncResultOf<T>(method: MethodOf<TDataService>, value?: T): void {
        this.getFor(method).and.returnValue(this.$promises.resolve(value));
    }

    /**
     * Mocks result of data service method which is synchronous.
     * @param method Method name.
     * @param value Optional value to be returned as part of resolved promise.
     */
    public mockSyncResultOf<T>(method: MethodOf<TDataService>, value?: T): void {
        this.getFor(method).and.returnValue(value);
    }

    /**
     * Mocks failure of data service method.
     * @param method Method name.
     * @param value Optional value to be returned as part of rejected promise.
     */
    public mockFailureOf<T>(method: MethodOf<TDataService>, value?: T): void {
        this.getFor(method).and.returnValue(this.$promises.reject(value));
    }
}

//  Constructable implementation of DataServiceMock<TDataService>.
class DataServiceMockImpl<TDataService> extends DataServiceMock<TDataService> {
    constructor(
        dataService: TDataService,
        $promises: ng.IQService) {

        super(dataService, $promises);
    }
}

/**
 * Combines mocks of shared data services.
 * NOTE: this class uses inject().
 */
export class DataServiceMocks {
    private _pdmsDataService: DataServiceMock<Pdms.IPdmsDataService>;
    private _variantDataService: DataServiceMock<IVariantDataService>;
    private _graphDataService: DataServiceMock<IGraphDataService>;
    private _manualRequestsDataService: DataServiceMock<IManualRequestsDataService>;
    private _cmsDataService: DataServiceMock<ICmsDataService>;
    private _groundControlDataService: DataServiceMock<IGroundControlDataService>;
    private _groundControlApiService: DataServiceMock<IGroundControlApiService>;

    constructor() {
        inject((
            _$q_: ng.IQService,
            _pdmsDataService_: Pdms.IPdmsDataService,
            _variantDataService_: IVariantDataService,
            _graphDataService_: IGraphDataService,
            _manualRequestsDataService_: IManualRequestsDataService,
            _cmsDataService_: ICmsDataService,
            _groundControlDataService_: IGroundControlDataService,
            _groundControlApiService_: IGroundControlApiService) => {

            this._pdmsDataService = new DataServiceMockImpl(_pdmsDataService_, _$q_);
            this._variantDataService = new DataServiceMockImpl(_variantDataService_, _$q_);
            this._graphDataService = new DataServiceMockImpl(_graphDataService_, _$q_);
            this._manualRequestsDataService = new DataServiceMockImpl(_manualRequestsDataService_, _$q_);
            this._cmsDataService = new DataServiceMockImpl(_cmsDataService_, _$q_);
            this._groundControlDataService = new DataServiceMockImpl(_groundControlDataService_, _$q_);
            this._groundControlApiService = new DataServiceMockImpl(_groundControlApiService_, _$q_);
        });
    }

    //  Gets PDMS data service mock.
    public get pdmsDataService(): DataServiceMock<Pdms.IPdmsDataService> {
        return this._pdmsDataService;
    }

    //  Gets PDMS data service mock.
    public get variantDataService(): DataServiceMock<IVariantDataService> {
        return this._variantDataService;
    }

    //  Gets Graph data service mock.
    public get graphDataService(): DataServiceMock<IGraphDataService> {
        return this._graphDataService;
    }

    //  Gets Manual Requests data service mock.
    public get manualRequestsDataService(): DataServiceMock<IManualRequestsDataService> {
        return this._manualRequestsDataService;
    }

    //  Gets CMS data service mock.
    public get cmsDataService(): DataServiceMock<ICmsDataService> {
        return this._cmsDataService;
    }

    //  Gets Ground Control data service mock.
    public get groundControlDataService(): DataServiceMock<IGroundControlDataService> {
        return this._groundControlDataService;
    }

    //  Gets Ground Control api service mock.
    public get groundControlApiService(): DataServiceMock<IGroundControlApiService> {
        return this._groundControlApiService;
    }
}
