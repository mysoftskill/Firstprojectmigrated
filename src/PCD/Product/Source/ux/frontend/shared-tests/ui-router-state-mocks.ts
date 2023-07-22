import { SpyCache } from "./spy-cache";

/**
 * Mock of Angular UI router $state service.
 */
export class UiRouterStateMock {
    /**
     *  Actual $state service instance. 
     **/
    private $state: ng.ui.IStateService;

    /**
     *  Cache of Jasmine spies for service. 
     **/
    private readonly spiesImpl: SpyCache<ng.ui.IStateService>;

    constructor() {
        inject((_$state_: ng.ui.IStateService, _$rootScope_: ng.IRootScopeService) => {
            this.$state = _$state_;

            //  This mock sets up fails on calls to go() and transitionTo(). At the very beginning, however, Angular 
            //  sends $locationChangeSuccess event, which triggers UI router state change, thus failing the test case.
            //  Triggered digest cycle will remove $locationChangeSuccess event from the queue, thus avoiding the problem.
            _$rootScope_.$digest();
        });

        this.spiesImpl = new SpyCache(this.$state);

        //  Setup default spy behaviors.
        this.spiesImpl.failIfCalled("go");
        this.spiesImpl.failIfCalled("transitionTo");
        this.spiesImpl.failIfCalled("reload");
    }

    //  Gets $state service instance.
    public get instance(): ng.ui.IStateService {
        return this.$state;
    }

    //  Gets access to service spies.
    public get spies(): SpyCache<ng.ui.IStateService> {
        return this.spiesImpl;
    }
}
