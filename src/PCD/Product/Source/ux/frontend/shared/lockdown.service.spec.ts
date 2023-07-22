import * as angular from "angular";
import { TestSpec, SpyCache } from "../shared-tests/spec.base";

import { AppConfig } from "../module/data.module";
import { ILockdownService } from "./lockdown.service";

describe("lockdownService", () => {
    describe("defaults", () => {
        let spec: TestSpec;
        let lockdown: ILockdownService;

        beforeEach(() => {
            spec = new TestSpec();

            inject((_lockdownService_: ILockdownService) => {
                lockdown = _lockdownService_;
            });
        });

        it("indicates that lockdown is not active", () => {
            expect(lockdown.isActive()).toBe(false);
            expect(lockdown.getMessage()).toBeFalsy();
        });
    });

    describe("when lockdown is configured to be active", () => {
        let spec: TestSpec;
        let lockdown: ILockdownService;

        beforeEach(() => {
            spec = new TestSpec();

            //  Inject AppConfig, so the actual object in use by Angular would be modified.
            inject((_appConfig_: AppConfig) => {
                _appConfig_.lockdown = {
                    isActive: true,
                    //  Actual dates do not matter. Date logic is handled in the Web Role and
                    //  frontend trusts the values are correct.
                    startedUtc: "2000-01-01",
                    endedUtc: "2001-01-01"
                };
            });

            inject((_lockdownService_: ILockdownService) => {
                lockdown = _lockdownService_;
            });
        });

        it("indicates that lockdown is active", () => {
            expect(lockdown.isActive()).toBe(true);
            expect(lockdown.getMessage()).toBeTruthy();
        });
    });
});
