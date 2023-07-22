import * as angular from "angular";
import { TestSpec, SpyCache } from "../shared-tests/spec.base";

import { IIdService } from "./id.service";

describe("ID service", () => {
    let spec: TestSpec;
    let idService: IIdService;

    beforeEach(() => {
        spec = new TestSpec();

        inject((_idService_: IIdService) => {
            idService = _idService_;
        });
    });

    it("returns new ID on each call to getNextId()", () => {
        let id1 = idService.getNextId();
        expect(id1).toBeTruthy();

        let id2 = idService.getNextId();
        expect(id2).toBeTruthy();
        expect(id2).not.toEqual(id1);
    });

    it("throws if generateGuid() is called", () => {
        expect(() => idService.generateGuid()).toThrow();
    });
});
