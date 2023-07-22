import { TestSpec, ComponentInstance, SpyCache } from "../../../shared-tests/spec.base";

import DirectoryResourceViewComponent from "./directory-resource-view.component";

describe("DirectoryResourceViewComponent", () => {
    let spec: TestSpec;

    beforeEach(() => {
        spec = new TestSpec();
    });

    describe("mailing action", () => {
        it("for email-able resources", () => {
            // act
            let component = createComponentForAdmins();

            // assert
            expect(component.instance.getResourceClass(component.instance.resources[0])).toBe("interaction-enabled");
            expect(component.instance.canMailAllResources()).toBeTruthy();
            expect(component.instance.isResourceMailEnabled(component.instance.resources[0])).toBeTruthy();
            expect(component.instance.isResourceMailEnabled(component.instance.resources[1])).toBeTruthy();
            expect(component.instance.getRecipients(...component.instance.resources)).toBe("joe;jane");
        });

        it("for non-email-able resources", () => {
            // arrange
            spec.dataServiceMocks.graphDataService.mockAsyncResultOf("getSecurityGroupById",
                { displayName: "PCD FAKE", email: "PCD_FAKE@example.com", isInvalid: false });

            // act
            let component = createComponentForSecurityGroups();

            // assert
            expect(component.instance.getResourceClass(component.instance.resources[0])).toBe("interaction-disabled");
            expect(component.instance.canMailAllResources()).toBeFalsy();
            expect(component.instance.isResourceMailEnabled(component.instance.resources[0])).toBeFalsy();
        });


        it("for invalid resources", () => {
            // arrange
            spec.dataServiceMocks.graphDataService.mockFailureOf("getSecurityGroupById", {});

            // act
            let component = createComponentForSecurityGroups();

            // assert
            expect(component.instance.getResourceClass(component.instance.resources[0])).toBe("invalid");
            expect(component.instance.canMailAllResources()).toBeFalsy();
            expect(component.instance.isResourceMailEnabled(component.instance.resources[0])).toBeFalsy();
        });


        function createComponentForAdmins(): ComponentInstance<DirectoryResourceViewComponent> {
            let serviceAdmins: string[] = ["joe", "jane"];

            return spec.createComponent<DirectoryResourceViewComponent>({
                markup: `<pcd-directory-resource-view
                            ng-model=serviceAdmins
                            type="named-resource"
                            actionable-kind="email"
                            resource-label="Admins"></pcd-directory-resource-view>`,
                data: {
                    serviceAdmins: serviceAdmins
                }
            });
        }

        function createComponentForSecurityGroups(): ComponentInstance<DirectoryResourceViewComponent> {
            let writeSecurityGroups: string[] = ["7acb8ba8-4746-46bb-a7f0-39a52166e46f", "7acb8ba8-4746-46bb-a7f0-39a52166e46f"];

            return spec.createComponent<DirectoryResourceViewComponent>({
                markup: `<pcd-directory-resource-view
                            ng-model=writeSecurityGroups
                            type="security-group"
                            resource-label="Write security groups"></pcd-directory-resource-view>`,
                data: {
                    writeSecurityGroups: writeSecurityGroups
                }
            });
        }
    });

    describe("for alert contacts", () => {
        it("obtains the correct resources based on the set resource type", () => {
            spec.dataServiceMocks.graphDataService.mockAsyncResultOf("getContactByEmail",
                { displayName: "Boy Reynalds", email: "PCD_FAKE@example.com", isInvalid: false });

            // act
            let component = createComponent();

            // assert
            expect(component.instance.resources).toEqual([
                { displayName: "Boy Reynalds", email: "PCD_FAKE@example.com", isInvalid: false },
                { displayName: "Boy Reynalds", email: "PCD_FAKE@example.com", isInvalid: false }
            ]);
            expect(component.instance.isResourceMailEnabled(component.instance.resources[0])).toBeTruthy();
        });

        function createComponent(): ComponentInstance<DirectoryResourceViewComponent> {
            let alertContacts: string[] = ["breynalds0@cloudflare.com", "breynalds0@cloudflare.com"];

            return spec.createComponent<DirectoryResourceViewComponent>({
                markup: `<pcd-directory-resource-view
                            ng-model=alertContacts
                            type="email"
                            resource-label="Alert contacts"></pcd-directory-resource-view>`,
                data: {
                    alertContacts: alertContacts
                }
            });
        }
    });

    describe("for announcement contacts", () => {
        it("obtains the correct resources based on the set resource type", () => {
            spec.dataServiceMocks.graphDataService.mockAsyncResultOf("getContactByEmail",
                { displayName: "Jonie Dobie", email: "PCD_FAKE@example.com", isInvalid: false });

            // act
            let component = createComponent();

            // assert
            expect(component.instance.resources).toEqual([
                { displayName: "Jonie Dobie", email: "PCD_FAKE@example.com", isInvalid: false },
                { displayName: "Jonie Dobie", email: "PCD_FAKE@example.com", isInvalid: false }
            ]);
            expect(component.instance.isResourceMailEnabled(component.instance.resources[0])).toBeTruthy();
        });

        function createComponent(): ComponentInstance<DirectoryResourceViewComponent> {
            let announcementContacts: string[] = ["jdobie1@multiply.com", "jdobie1@multiply.com"];


            return spec.createComponent<DirectoryResourceViewComponent>({
                markup: `<pcd-directory-resource-view
                            ng-model=announcementContacts
                            type="email"
                            resource-label="Announcement contacts"></pcd-directory-resource-view>`,
                data: {
                    announcementContacts: announcementContacts
                }
            });
        }
    });

    describe("for write security groups", () => {
        it("obtains the correct resources based on the set resource type", () => {
            spec.dataServiceMocks.graphDataService.mockAsyncResultOf("getSecurityGroupById",
                { displayName: "PCD FAKE", email: "PCD_FAKE@example.com", isInvalid: false });

            // act
            let component = createComponent();

            // assert
            expect(component.instance.resources).toEqual([
                { displayName: "PCD FAKE", email: "PCD_FAKE@example.com", isInvalid: false },
                { displayName: "PCD FAKE", email: "PCD_FAKE@example.com", isInvalid: false }
            ]);
        });

        it("correctly identifies an invalid group", () => {
            spec.dataServiceMocks.graphDataService.mockFailureOf("getSecurityGroupById", {});

            // act
            let component = createComponent();

            // assert
            expect(component.instance.resources).toEqual([{
                displayName: "7acb8ba8-4746-46bb-a7f0-39a52166e46f",
                email: "",
                isInvalid: true
            }, {
                displayName: "7acb8ba8-4746-46bb-a7f0-39a52166e46f",
                email: "",
                isInvalid: true
            }]);
        });

        function createComponent(): ComponentInstance<DirectoryResourceViewComponent> {
            let writeSecurityGroups: string[] = ["7acb8ba8-4746-46bb-a7f0-39a52166e46f", "7acb8ba8-4746-46bb-a7f0-39a52166e46f"];

            return spec.createComponent<DirectoryResourceViewComponent>({
                markup: `<pcd-directory-resource-view
                            ng-model=writeSecurityGroups
                            type="security-group"
                            resource-label="Write security groups"></pcd-directory-resource-view>`,
                data: {
                    writeSecurityGroups: writeSecurityGroups
                }
            });
        }
    });
});

