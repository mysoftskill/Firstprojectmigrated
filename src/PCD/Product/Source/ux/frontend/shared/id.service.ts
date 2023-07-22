import { Service, Inject } from "../module/app.module";

//  Provides facilities for generating new IDs.
export interface IIdService {
    //  Gets a new, monotonically grown, ID.
    getNextId(): string;

    //  Generates new GUID.
    generateGuid(): string;
}

@Service({
    name: "idService"
})
class IdService implements IIdService {
    private nextId = 0;

    //  Part of IIdService.
    public getNextId(): string {
        this.nextId++;
        return this.nextId.toString();
    }

    //  Part of IIdService.
    public generateGuid(): string {
        throw new Error("Not implemented.");
    }
}
