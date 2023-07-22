import { TestSpec } from "../shared-tests/spec.base";
import { appModule } from "../module/app.module";

import "./app.misc";

describe("Additional app configuration", () => {
    let $qProvider: ng.IQProvider;
    let $oneUiDefaults: MeePortal.OneUI.Angular.OneUiDefaults;

    beforeEach(() => {
        new TestSpec({ doNotRunInject: true });

        //  The only time we can access provider is during configuration phase. Use this as a chance to save 
        //  reference to provider instance.
        appModule.config(["$qProvider", "$oneUiDefaults", (_$qProvider_: ng.IQProvider, _$oneUiDefaults_: MeePortal.OneUI.Angular.OneUiDefaults) => {
            $qProvider = _$qProvider_;
            $oneUiDefaults = _$oneUiDefaults_;
        }]);

        //  Kick off app initialization cycle.
        inject(() => { });
    });

    it("disables errors on unhandled rejections", () => {
        expect($qProvider.errorOnUnhandledRejections()).toEqual(false);
    });

    it("configures default mee-paragraph style", () => {
        expect($oneUiDefaults.paragraphStyle).toBe("para4");
    });
});
