import * as angular from "angular";
import { TestSpec, SpyCache } from "../shared-tests/spec.base";
import * as Pdms from "../shared/pdms/pdms-types";

import { IServiceTreeDataService } from "./service-tree-data.service";

describe("Service Tree data service", () => {
    let spec: TestSpec;
    let serviceTreeDataService: IServiceTreeDataService;

    beforeEach(() => {
        spec = new TestSpec();

        inject((_serviceTreeDataService_: IServiceTreeDataService) => {
            serviceTreeDataService = _serviceTreeDataService_;
        });
    });

    it("get the correct link for a service", () => {
        const entity: Pdms.STEntityBase = {
            id: "519c81ef-bef8-4d2d-b1b4-9647c3a7dab2",
            kind: "service"
        };

        expect(serviceTreeDataService.getServiceURL(entity)).toBe("https://servicetree.msftcloudes.com/#/ServiceModel/Service/Profile/519c81ef-bef8-4d2d-b1b4-9647c3a7dab2");
    });

    it("get the correct link for a service group", () => {
        const entity: Pdms.STEntityBase = {
            id: "f24eb03a-b9b1-4718-a69f-8c8ea5929c1d",
            kind: "serviceGroup"
        };

        expect(serviceTreeDataService.getServiceURL(entity)).toBe("https://servicetree.msftcloudes.com/#/OrganizationModel/ServiceGroup/Profile/f24eb03a-b9b1-4718-a69f-8c8ea5929c1d");
    });

    it("get the correct link for a team group", () => {
        const entity: Pdms.STEntityBase = {
            id: "4c643d43-520a-47b1-90ca-2bfc652ababe",
            kind: "teamGroup"
        };

        expect(serviceTreeDataService.getServiceURL(entity)).toBe("https://servicetree.msftcloudes.com/#/OrganizationModel/TeamGroup/Profile/4c643d43-520a-47b1-90ca-2bfc652ababe");
    });
});

