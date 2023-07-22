import * as angular from "angular";
import { TestSpec, SpyCache } from "../shared-tests/spec.base";

import { IOwnerIdContextService } from "./owner-id-context.service";

describe("Owner ID context service", () => {
    let spec: TestSpec;
    let ownerIdContextService: IOwnerIdContextService;

    beforeEach(() => {
        spec = new TestSpec();

        inject((_ownerIdContextService_: IOwnerIdContextService) => {
            ownerIdContextService = _ownerIdContextService_;
        });
    });

    it("returns an empty string if there is no active owner ID", () => {
        expect(ownerIdContextService.getActiveOwnerId()).toBe("");
    });

    it("returns most recently set active owner ID", () => {
        ownerIdContextService.setActiveOwnerId("07b5b571-81e3-4aca-bde8-7ddec0c98adc");
        expect(ownerIdContextService.getActiveOwnerId()).toBe("07b5b571-81e3-4aca-bde8-7ddec0c98adc");

        ownerIdContextService.setActiveOwnerId("3fdbfbcc-d7f7-4e88-9d11-f1fc154980fe");
        expect(ownerIdContextService.getActiveOwnerId()).toBe("3fdbfbcc-d7f7-4e88-9d11-f1fc154980fe");
    });
});
