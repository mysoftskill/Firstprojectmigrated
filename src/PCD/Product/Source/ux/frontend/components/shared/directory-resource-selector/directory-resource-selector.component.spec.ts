import { TestSpec, ComponentInstance } from "../../../shared-tests/spec.base";

import { SecurityGroupSelectorData, ContactSelectorData, VariantSelectorData, ApplicationSelectorData } from "./directory-resource-selector-types";
import DirectoryResourceSelectorComponent from "./directory-resource-selector.component";
import * as GraphTypes from "../../../shared/graph/graph-types";
import { VariantSelector } from "./selectors/variant-selector";

describe("Resource selector", () => {
    let spec: TestSpec;
    let $timeout: ng.ITimeoutService;

    beforeEach(() => {
        spec = new TestSpec();
        inject((_$timeout_: ng.ITimeoutService) => {
            $timeout = _$timeout_;
        });
    });

    describe("for security groups", () => {
        beforeEach(() => {
            spec.dataServiceMocks.graphDataService.mockAsyncResultOf("getSecurityGroupById", {
                id: "OSG_ID",
                email: "osg@microsoft.com",
                displayName: "OSG",
            });
        });

        it("sets the labels appropriately based on resource type", () => {
            // act
            let component = createComponentWithData({
                securityGroups: [{
                    id: "OSG_ID",
                    email: "osg@microsoft.com",
                    displayName: "OSG",
                    isInvalid: false
                }, {
                    id: "WDG_ID",
                    email: "wdg@microsoft.com",
                    displayName: "WDG",
                    isInvalid: false
                }]
            });

            // assert
            expect(component.instance.displayLabel).toEqual("Write security groups");
            expect(component.instance.noSuggestionLabel).toEqual(
                component.instance.resourceSelector.getDefaultStrings().noSuggestionLabel);
        });

        it("adds resource on add click", () => {
            // arrange
            let component = createComponentWithData({
                securityGroups: [{
                    id: "OSG_ID",
                    email: "osg@microsoft.com",
                    displayName: "OSG",
                    isInvalid: false
                }, {
                    id: "WDG_ID",
                    email: "wdg@microsoft.com",
                    displayName: "WDG",
                    isInvalid: false
                }]
            });
            spec.dataServiceMocks.graphDataService.mockSyncResultOf("isSecurityGroupNameValid", true);
            spec.dataServiceMocks.graphDataService.mockSyncResultOf("getSecurityGroupFromCache", {
                id: "MEE_ID",
                email: "mee@microsoft.com",
                displayName: "MEE",
            });

            // act
            component.instance.addResource("Mee");

            // assert
            expect(component.instance.addedResources[2].displayName).toEqual("MEE");
            expect((<SecurityGroupSelectorData> component.instance.ngModel).securityGroups[2].id).toEqual("MEE_ID");
        });

        it("removes resource on close button click", () => {
            // arrange
            let component = createComponentWithData({
                securityGroups: [{
                    id: "OSG_ID",
                    email: "osg@microsoft.com",
                    displayName: "OSG",
                    isInvalid: false
                }]
            });

            // act
            component.instance.removeSelectedResource("OSG_ID");
            component.scope.$digest();

            // assert
            expect(component.instance.addedResources[0]).toBeUndefined();
            expect((<SecurityGroupSelectorData>component.instance.ngModel).securityGroups[0]).toBeUndefined();
        });

        it("gets suggestions to be shown in drop down list", (done: DoneFn) => {
            // arrange
            let component = createComponentWithData({
                securityGroups: [{
                    id: "OSG_ID",
                    email: "osg@microsoft.com",
                    displayName: "OSG",
                    isInvalid: false
                }, {
                    id: "WDG_ID",
                    email: "wdg@microsoft.com",
                    displayName: "WDG",
                    isInvalid: false
                }]
            });
            let groups: GraphTypes.Group[] = [{
                displayName: "OSG",
                id: "id1",
                email: "id1-email@email.com",
                securityEnabled: true,
                isInvalid: false
            }, {
                displayName: "OSG_FTE",
                id: "id2",
                email: null,
                securityEnabled: true,
                isInvalid: false
            }];
            spec.dataServiceMocks.graphDataService.mockAsyncResultOf("getSecurityGroupsWithPrefix", groups);

            // act
            component.instance.getSuggestions("OSG")
                .then((results: any) => {

                    // assert
                    expect(results[0].type).toEqual("string");
                    expect(results[0].value).toEqual("OSG (id1-email@email.com)");

                    expect(results[1].type).toEqual("string");
                    expect(results[1].value).toEqual("OSG_FTE");

                    done();
                });
            component.scope.$digest();
        });

        function createComponentWithData(selectorData: SecurityGroupSelectorData): ComponentInstance<DirectoryResourceSelectorComponent> {
            let component = spec.createComponent<DirectoryResourceSelectorComponent>({
                markup: `<pcd-directory-resource-selector 
                                pcd-error-id="errorId"
                                pcd-selector-label="Write security groups" 
                                pcd-resource-type="security-group"
                                ng-model="selectorData"></pcd-directory-resource-selector>`,
                data: { selectorData }
            });
            $timeout.flush();  // this is required to clear timeout and execute initializeResources instantly

            return component;
        }
    });

    describe("for applications", () => {
        beforeEach(() => {
            spec.dataServiceMocks.graphDataService.mockAsyncResultOf("getApplicationById", {
                id: "82eda70a-0b6c-4a53-ab77-9b62c646677b",
                displayName: "Pdms_sev1",
                isInvalid: false
            });
        });

        it("sets the labels appropriately based on resource type", () => {
            // act
            let component = createComponentWithData({
                applications: [
                    { id: "82eda70a-0b6c-4a53-ab77-9b62c646677b", displayName: "Pdms_sev1", isInvalid: false },
                    { id: "56eda70a-0b6c-4a53-ab77-9b62c646677b", displayName: "Pdms_sev2", isInvalid: false }
                ]
            });

            // assert
            expect(component.instance.displayLabel).toEqual("Tagging applications");
            expect(component.instance.noSuggestionLabel).toEqual(
                component.instance.resourceSelector.getDefaultStrings().noSuggestionLabel);
        });

        it("adds resource on add click", () => {
            // arrange
            let component = createComponentWithData({
                applications: [
                    { id: "82eda70a-0b6c-4a53-ab77-9b62c646677b", displayName: "Pdms_sev1", isInvalid: false },
                    { id: "56eda70a-0b6c-4a53-ab77-9b62c646677b", displayName: "Pdms_sev2", isInvalid: false }
                ]
            });
            spec.dataServiceMocks.graphDataService.mockSyncResultOf("isApplicationNameValid", true);
            spec.dataServiceMocks.graphDataService.mockSyncResultOf("getApplicationFromCache", {
                id: "56jua70a-0b6c-4a53-ab77-9b62c646677b",
                displayName: "MEE",
            });

            // act
            component.instance.addResource("Mee");

            // assert
            expect(component.instance.addedResources[2].displayName).toEqual("MEE");
            expect((<ApplicationSelectorData>component.instance.ngModel).applications[2].id).toEqual("56jua70a-0b6c-4a53-ab77-9b62c646677b");
        });

        it("removes resource on close button click", () => {
            // arrange
            let component = createComponentWithData({
                applications: [
                    { id: "82eda70a-0b6c-4a53-ab77-9b62c646677b", displayName: "Pdms_sev1", isInvalid: false },
                ]
            });

            // act
            component.instance.removeSelectedResource("82eda70a-0b6c-4a53-ab77-9b62c646677b");

            // assert
            expect(component.instance.addedResources[0]).toBeUndefined();
            expect((<ApplicationSelectorData>component.instance.ngModel).applications[0]).toBeUndefined();
        });

        it("gets suggestions to be shown in drop down list", (done: DoneFn) => {
            // arrange
            let component = createComponentWithData({
                applications: [
                    { id: "82eda70a-0b6c-4a53-ab77-9b62c646677b", displayName: "OSG", isInvalid: false },
                    { id: "56eda70a-0b6c-4a53-ab77-9b62c646677b", displayName: "OSG_FTE", isInvalid: false }
                ]
            });
            let applications: GraphTypes.Application[] = [{
                displayName: "OSG",
                id: "id1",
                isInvalid: false
            }, {
                displayName: "OSG_FTE",
                id: "id2",
                isInvalid: false
            }];
            spec.dataServiceMocks.graphDataService.mockAsyncResultOf("getApplicationsWithPrefix", applications);

            // act
            component.instance.getSuggestions("OSG")
                .then((results: any) => {

                    // assert
                    expect(results[0].type).toEqual("string");
                    expect(results[0].value).toEqual("OSG");

                    expect(results[1].type).toEqual("string");
                    expect(results[1].value).toEqual("OSG_FTE");

                    done();
                });
            component.scope.$digest();
        });

        function createComponentWithData(selectorData: ApplicationSelectorData): ComponentInstance<DirectoryResourceSelectorComponent> {
            let component = spec.createComponent<DirectoryResourceSelectorComponent>({
                markup: `<pcd-directory-resource-selector
                                pcd-error-id="errorId"
                                pcd-selector-label="Tagging applications"
                                pcd-resource-type="application"
                                ng-model="selectorData"></pcd-directory-resource-selector>`,
                data: { selectorData }
            });
            $timeout.flush();  // this is required to clear timeout and execute initializeResources instantly

            return component;
        }
    });

    describe("for contacts", () => {
        let jackContact: GraphTypes.Contact;
        let jamesContact: GraphTypes.Contact;

        beforeEach(() => {
            jackContact = {
                id: "JACK_ID",
                displayName: "JACK",
                email: "jack@microsoft.com",
                isInvalid: false
            };
            jamesContact = {
                id: "JAMES_ID",
                displayName: "JAMES",
                email: "boringj@microsoft.com",
                isInvalid: false
            };
        });

        it("sets the labels appropriately based on resource type", () => {
            // act
            spec.dataServiceMocks.graphDataService.mockAsyncResultOf("getContactByEmail", jackContact);
            let component = createComponentWithData({
                contacts: [jackContact, jamesContact]
            });

            // assert
            expect(component.instance.displayLabel).toEqual("Alert contacts");
            expect(component.instance.noSuggestionLabel).toEqual(
                component.instance.resourceSelector.getDefaultStrings().noSuggestionLabel);
        });

        it("adds resource on add click", () => {
            // arrange
            spec.dataServiceMocks.graphDataService.mockAsyncResultOf("getContactByEmail", jackContact);
            let component = createComponentWithData({
                contacts: [jackContact]
            });

            spec.dataServiceMocks.graphDataService.mockAsyncResultOf("getContactById", {});
            spec.dataServiceMocks.graphDataService.mockSyncResultOf("isContactNameValid", true);
            spec.dataServiceMocks.graphDataService.mockAsyncResultOf("getContactByEmail", jamesContact);
            spec.dataServiceMocks.graphDataService.mockSyncResultOf("getContactFromCache", jamesContact);

            // act
            component.instance.addResource("James (james@microsoft.com)");

            // assert
            expect(component.instance.addedResources[1].displayName).toEqual("JAMES");
            expect((<ContactSelectorData> component.instance.ngModel).contacts[1].id).toEqual("JAMES_ID");
            expect((<ContactSelectorData> component.instance.ngModel).contacts[1].email).toEqual("boringj@microsoft.com");
        });

        it("adds resource (with brackets in name) on add click", () => {
            // arrange
            jamesContact = {
                id: "JAMES_ID",
                displayName: "JAMES (with a J)",
                email: "boringj@microsoft.com",
                isInvalid: false
            };
            spec.dataServiceMocks.graphDataService.mockAsyncResultOf("getContactByEmail", jackContact);
            let component = createComponentWithData({
                contacts: [jackContact]
            });

            spec.dataServiceMocks.graphDataService.mockAsyncResultOf("getContactById", {});
            spec.dataServiceMocks.graphDataService.mockSyncResultOf("isContactNameValid", true);
            spec.dataServiceMocks.graphDataService.mockAsyncResultOf("getContactByEmail", jamesContact);
            spec.dataServiceMocks.graphDataService.mockSyncResultOf("getContactFromCache", jamesContact);

            // act
            component.instance.addResource("James (with a J) (james@microsoft.com)");

            // assert
            expect(component.instance.addedResources[1].displayName).toEqual("JAMES (with a J)");
            expect((<ContactSelectorData> component.instance.ngModel).contacts[1].id).toEqual("JAMES_ID");
            expect((<ContactSelectorData> component.instance.ngModel).contacts[1].email).toEqual("boringj@microsoft.com");
        });

        it("removes resource on close button click", () => {
            // arrange
            spec.dataServiceMocks.graphDataService.mockAsyncResultOf("getContactByEmail", jackContact);
            let component = createComponentWithData({
                contacts: [jackContact]
            });

            // act
            component.instance.removeSelectedResource("JACK_ID");

            // assert
            expect(component.instance.addedResources[0]).toBeUndefined();
            expect((<ContactSelectorData>component.instance.ngModel).contacts[0]).toBeUndefined();
        });

        it("gets suggestions to be shown in drop down list", (done: DoneFn) => {
            // arrange
            let contacts: GraphTypes.Contact[] = [jackContact, jamesContact];
            spec.dataServiceMocks.graphDataService.mockAsyncResultOf("getContactsWithPrefix", contacts);
            spec.dataServiceMocks.graphDataService.mockAsyncResultOf("getContactByEmail", jackContact);
            let component = createComponentWithData({ contacts });

            // act
            component.instance.getSuggestions("J")
                .then((results: any) => {

                    // assert
                    expect(results[0].type).toEqual("string");
                    expect(results[0].value).toEqual("JACK (jack@microsoft.com)");

                    expect(results[1].type).toEqual("string");
                    expect(results[1].value).toEqual("JAMES (boringj@microsoft.com)");

                    done();
                });
            component.scope.$digest();
        });

        function createComponentWithData(selectorData: ContactSelectorData): ComponentInstance<DirectoryResourceSelectorComponent> {
            let component = spec.createComponent<DirectoryResourceSelectorComponent>({
                markup: `<pcd-directory-resource-selector 
                            pcd-error-id="errorId"
                            pcd-selector-label="Alert contacts" 
                            pcd-resource-type="contact" 
                            ng-model="selectorData"></pcd-directory-resource-selector>`,
                data: { selectorData }
            });

            $timeout.flush();  // this is required to clear timeout and execute initializeResources instantly
            return component;
        }
    });

    describe("for variants", () => {
        beforeEach(() => {
            spec.dataServiceMocks.variantDataService.mockAsyncResultOf("getVariants", [{
                id: "VARIANT1_ID",
                name: "VARIANT1",
            }, {
                id: "VARIANT2_ID",
                name: "VARIANT2",
            }, {
                id: "VARIANT3_ID",
                name: "VARIANT3",
            }]);
        });

        it("sets the labels appropriately based on resource type", () => {
            // act
            let component = createComponentWithData({
                variants: [
                    { id: "VARIANT1_ID", displayName: "VARIANT1" },
                    { id: "VARIANT2_ID", displayName: "VARIANT2" }
                ]
            });

            // assert
            expect(component.instance.displayLabel).toEqual("Alert variants");
            expect(component.instance.noSuggestionLabel).toEqual(
                component.instance.resourceSelector.getDefaultStrings().noSuggestionLabel);
        });

        it("adds resource on add click", () => {
            // arrange
            let component = createComponentWithData({
                variants: [
                    { id: "VARIANT1_ID", displayName: "VARIANT1" },
                    { id: "VARIANT2_ID", displayName: "VARIANT2" }
                ]
            });
            component.instance.addedResources = [{
                id: "VARIANT1_ID",
                displayName: "VARIANT1",
                isInvalid: false
            }, {
                id: "VARIANT2_ID",
                displayName: "VARIANT2",
                isInvalid: false
            }];
            (<VariantSelector>component.instance.resourceSelector).setNgModel({
                variants: [{
                    id: "VARIANT1_ID",
                    displayName: "VARIANT1",
                }, {
                    id: "VARIANT2_ID",
                    displayName: "VARIANT2",
                }]
            });
            
            // act
            component.instance.addResource("VARIANT3");

            // assert
            expect(component.instance.addedResources[2].displayName).toEqual("VARIANT3");
            expect((<VariantSelectorData> component.instance.ngModel).variants[2].id).toEqual("VARIANT3_ID");
            expect((<VariantSelectorData> component.instance.ngModel).variants[2].displayName).toEqual("VARIANT3");
        });

        it("removes resource on close button click", () => {
            // arrange
            let component = createComponentWithData({
                variants: [
                    { id: "VARIANT1_ID", displayName: "VARIANT1" },
                ]
            });
            component.instance.addedResources = [{
                id: "VARIANT1_ID",
                displayName: "VARIANT1",
                isInvalid: false
            }];

            // act
            component.instance.removeSelectedResource("VARIANT1_ID");

            // assert
            expect(component.instance.addedResources[0]).toBeUndefined();
            expect((<VariantSelectorData>component.instance.ngModel).variants[0]).toBeUndefined();
        });

        it("gets suggestions to be shown in drop down list", (done: DoneFn) => {
            // arrange
            spec.dataServiceMocks.variantDataService.mockAsyncResultOf("getVariants", [{
                id: "VARIANT1_ID",
                name: "VARIANT1",
            }, {
                id: "VARIANT2_ID",
                name: "VARIANT2",
            }, {
                id: "VARIANT1_ID",
                name: "VARIANT11",
            }]);

            let component = createComponentWithData({
                variants: [
                    { id: "VARIANT1_ID", displayName: "VARIANT1" },
                    { id: "VARIANT2_ID", displayName: "VARIANT2" },
                    { id: "VARIANT11_ID", displayName: "VARIANT11" }
                ]
            });

            // act
            component.instance.getSuggestions("VARIANT1")
                .then((results: any) => {

                    // assert
                    expect(results[0].type).toEqual("string");
                    expect(results[0].value).toEqual("VARIANT1");

                    expect(results[1].type).toEqual("string");
                    expect(results[1].value).toEqual("VARIANT11");

                    done();
                });
            component.scope.$digest();
        });

        function createComponentWithData(selectorData: VariantSelectorData): ComponentInstance<DirectoryResourceSelectorComponent> {
            let component = spec.createComponent<DirectoryResourceSelectorComponent>({
                markup: `<pcd-directory-resource-selector 
                            pcd-error-id="errorId"
                            pcd-selector-label="Alert variants" 
                            pcd-resource-type="variant" 
                            ng-model="selectorData"></pcd-directory-resource-selector>`,
                data: { selectorData }
            });

            $timeout.flush();  // this is required to clear timeout and execute initializeResources instantly
            return component;
        }
    });
});

