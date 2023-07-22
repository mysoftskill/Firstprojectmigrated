import { Inject, Service } from "../module/app.module";
import { IStringFormatFilter } from "../shared/filters/string-format.filter";
import * as Pdms from "../shared/pdms/pdms-types";

export const ServiceTreeBaseUrl = "https://servicetree.msftcloudes.com";

export interface IServiceTreeDataService {
    //  Gets the URL for Service Tree for a specific service.
    getServiceURL(serviceEntity: Pdms.STEntityBase): string;
}

@Service({
    name: "serviceTreeDataService"
})
@Inject("stringFormatFilter")
class ServiceTreeDataService implements IServiceTreeDataService {
    private readonly urlFormatPerServiceTreeIdKind: {
        [key in Pdms.STEntityKind]: string
    } = {
        "service": "{0}/#/ServiceModel/Service/Profile/{1}",
        "teamGroup": "{0}/#/OrganizationModel/TeamGroup/Profile/{1}",
        "serviceGroup": "{0}/#/OrganizationModel/ServiceGroup/Profile/{1}"
    };

    constructor(
        private readonly formatString: IStringFormatFilter) {
    }

    public getServiceURL(serviceEntity: Pdms.STEntityBase): string {
        return this.formatString(this.urlFormatPerServiceTreeIdKind[serviceEntity.kind], [ServiceTreeBaseUrl, serviceEntity.id]);
    }
}