import { TestSpec, ComponentInstance, SpyCache } from "../../../../../shared-tests/spec.base";
import LinkServiceTreeTeamComponent from "./link-team.component";
import { STServiceDetails } from "../../../../../shared/pdms/pdms-types";
import ServiceTreeSelectorComponent from "../../../../shared/service-tree-selector/service-tree-selector.component";

describe("LinkServiceTreeTeamComponent", () => {
    let spec: TestSpec;
    let service: STServiceDetails;
    let serviceTreeSpy: SpyCache<ServiceTreeSelectorComponent>;

    beforeEach(() => {
        spec = new TestSpec();

        service = {
            id: "",
            name: "Service",
            description: "",
            serviceAdmins: [], 
            organizationId: "",
            divisionId: "",
            kind: "service"
        };

        let registryServiceSpy = new SpyCache(spec.$meeComponentRegistry);
        let serviceTree = new ServiceTreeSelectorComponent(null, null, null, spec.$meeComponentRegistry);

        serviceTreeSpy = new SpyCache(serviceTree);

        registryServiceSpy.getFor("getInstanceById").and.returnValue(serviceTree);
    });

    describe("admin info banner", () => {
        it("should not be shown when a service has not been selected", () => {
            // arrange
            let component = createComponent();

            // act/assert
            expect(component.instance.showAdminInfoBanner()).toBeFalsy();
        });

        it("should be shown when a service has been selected and user is not an admin of the service", () => {
            // arrange
            serviceTreeSpy.getFor("isAdminOfService").and.returnValue(false);

            let component = createComponent();
            component.instance.service = service;

            // act/assert
            expect(component.instance.showAdminInfoBanner()).toBeTruthy();
        });
    });

    describe("link service button", () => {
        it("is disabled when a service has not been selected", () => {
            // arrange
            let component = createComponent();
            
            // act/assert
            expect(component.instance.canLinkService()).toBeFalsy();
        });

        it("is enabled when a service has been selected and user is an admin of the service", () => {
            // arrange
            serviceTreeSpy.getFor("isAdminOfService").and.returnValue(true);

            let component = createComponent();
            component.instance.service = service;

            // act/assert
            expect(component.instance.canLinkService()).toBeTruthy();
        });
    });


    function createComponent(): ComponentInstance<LinkServiceTreeTeamComponent> {
        return spec.createComponent<LinkServiceTreeTeamComponent>({
            markup: `<pcd-link-service-tree-team></pcd-link-service-tree-team>`
        });
    }
});
