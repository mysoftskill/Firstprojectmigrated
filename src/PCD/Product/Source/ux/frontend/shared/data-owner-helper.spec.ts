import { TestSpec } from "../shared-tests/spec.base";

import * as Pdms from "./pdms/pdms-types";
import * as Guid from "./guid";
import { DataOwnerHelper } from "./data-owner-helper";

describe("Data owner helper", () => {
    let spec: TestSpec;
    let dataOwner: Pdms.DataOwner;
    let serviceTreeDetails: Pdms.STServiceDetails;

    beforeEach(() => {
        spec = new TestSpec();
        serviceTreeDetails = {
            id: "",
            name: "",
            description: "",
            serviceAdmins: [],
            organizationId: "OrgID",
            divisionId: "DivID",
            kind: "serviceGroup"
        };
        dataOwner = {
            id: "",
            name: "",
            description: "",
            alertContacts: [],
            announcementContacts: [],
            sharingRequestContacts: [],
            assetGroups: [],
            dataAgents: [],
            writeSecurityGroups: [],
            serviceTree: null
        };
    });

    describe("hasWriteSecurityGroups", () => {
        it("returns true when there is a write security group", () => {
            dataOwner.writeSecurityGroups = ["anyId"];

            expect(DataOwnerHelper.hasWriteSecurityGroups(dataOwner)).toBe(true);
        });
        it("returns false when there are no write security group", () => {
            dataOwner.writeSecurityGroups = [];

            expect(DataOwnerHelper.hasWriteSecurityGroups(dataOwner)).toBe(false);
        });
    });

    describe("hasOrphanedServiceTeam", () => {
        it("returns false when there is no Service Tree link", () => {
            dataOwner.serviceTree = null;

            expect(DataOwnerHelper.hasOrphanedServiceTeam(dataOwner)).toBe(false);
        });
        it("returns false when there is a valid Service Tree link", () => {
            dataOwner.serviceTree = serviceTreeDetails;

            expect(DataOwnerHelper.hasOrphanedServiceTeam(dataOwner)).toBe(false);
        });
        it("returns true when there is an empty guid organization/division ID for that team", () => {
            serviceTreeDetails.organizationId = Guid.EmptyGuid;
            serviceTreeDetails.divisionId = Guid.EmptyGuid;
            dataOwner.serviceTree = serviceTreeDetails;

            expect(DataOwnerHelper.hasOrphanedServiceTeam(dataOwner)).toBe(true);
        });
    });

    describe("isValidDataOwner", () => {
        it("returns true when there is a write security group and a valid service tree link", () => {
            dataOwner.writeSecurityGroups = ["anyId"];
            dataOwner.serviceTree = serviceTreeDetails;

            expect(DataOwnerHelper.isValidDataOwner(dataOwner)).toBe(true);
        });
        it("returns false when there are no write security group", () => {
            dataOwner.writeSecurityGroups = [];

            expect(DataOwnerHelper.isValidDataOwner(dataOwner)).toBe(false);
        });
        it("returns false when there is no Service Tree link", () => {
            dataOwner.serviceTree = null;

            expect(DataOwnerHelper.isValidDataOwner(dataOwner)).toBe(false);
        });
        it("returns false for a null owner", () => {
            expect(DataOwnerHelper.isValidDataOwner(null)).toBe(false);
        });
    });
});
