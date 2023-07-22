import * as Pdms from "./pdms/pdms-types";
import * as Guid from "./guid";

export class DataOwnerHelper {
    //  Returns whether an owner has write security groups defined.
    public static hasWriteSecurityGroups(dataOwner: Pdms.DataOwner): boolean {
        return !!dataOwner && !!dataOwner.writeSecurityGroups && !!dataOwner.writeSecurityGroups.length;
    }

    //  Returns whether an owner is linked to an orphan Service Tree record.
    public static hasOrphanedServiceTeam(dataOwner: Pdms.DataOwner): boolean {
        return !!dataOwner && !!dataOwner.serviceTree &&
            dataOwner.serviceTree.organizationId === Guid.EmptyGuid &&
            dataOwner.serviceTree.divisionId === Guid.EmptyGuid;
    }

    //  Returns whether an owner is valid or not.
    public static isValidDataOwner(dataOwner: Pdms.DataOwner): boolean {
        return DataOwnerHelper.hasWriteSecurityGroups(dataOwner) && !DataOwnerHelper.hasOrphanedServiceTeam(dataOwner);
    }
}
