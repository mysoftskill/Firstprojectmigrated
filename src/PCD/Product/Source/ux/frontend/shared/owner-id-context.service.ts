import { Service, Inject } from "../module/app.module";

//  Provides facilities for managing the active owner ID.
export interface IOwnerIdContextService {
    //  Gets the ID of the active owner if it exists, otherwise returns an empty string.
    getActiveOwnerId(): string;

    //  Sets the ID of the active owner.
    setActiveOwnerId(id: string): void;
}

@Service({
    name: "ownerIdContextService"
})
class OwnerIdContextService implements IOwnerIdContextService {
    private activeOwnerId: string;

    //  Part of IOwnerContextService.
    public getActiveOwnerId(): string {
        return this.activeOwnerId || "";
    }

    //  Part of IOwnerContextService.
    public setActiveOwnerId(id: string): void {
        this.activeOwnerId = id;
    }
}
