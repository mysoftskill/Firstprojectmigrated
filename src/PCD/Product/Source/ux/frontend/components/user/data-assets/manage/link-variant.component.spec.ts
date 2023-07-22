import { TestSpec, ComponentInstance } from "../../../../shared-tests/spec.base";
import LinkVariantComponent from "./link-variant.component";
import * as Pdms from "../../../../shared/pdms/pdms-types";
import { SpyCache } from "../../../../shared-tests/spy-cache";
import { VariantLinkingContext } from "../../../shared/asset-group/asset-groups-manage.component";
import { IVariantDataService } from "../../../../shared/variant/variant-data.service";
import { DataServiceMock } from "../../../../shared-tests/data-service-mocks";

describe("LinkVariant", () => {
    let spec: TestSpec;
    let $modalState: SpyCache<MeePortal.OneUI.Angular.IModalStateService>;
    let assetGroups: Pdms.AssetGroup[];
    let modalData: VariantLinkingContext;

    beforeEach(() => {
        spec = new TestSpec();

        inject((
                _$meeModal_: MeePortal.OneUI.Angular.IModalStateService
            ) => {
                $modalState = new SpyCache(_$meeModal_);
        });
    });

    beforeEach(() => {
        assetGroups = [{
            id: "DataAsset1ID",
            qualifier: {
                props: {
                    AssetType: "DataAsset1Type",
                    propName: "propValue"
                }
            },
            ownerId: "1",
            deleteAgentId: "DataAgent1ID",
            exportAgentId: "DataAgent2ID"
        },
        {
            id: "DataAsset2ID",
            qualifier: {
                props: {
                    AssetType: "DataAsset2Type",
                    propName: "propValue"
                }
            },
            ownerId: "1",
            deleteAgentId: "DataAgent2ID",
            exportAgentId: "DataAgent1ID"
        }];

        modalData = {
            variants: [{id: "variant1", displayName: "variantName1"}],
            ownerId: "owner1",
            assetGroups: assetGroups,
            onComplete: () => {}
        };

        $modalState.getFor("getData").and.returnValue(modalData);
    });

    describe("hasDataEntryErrors() requires", () => {
        it("tfsTrackingUri to be set", () => {
            let component = createComponent();

            component.instance.formData.tfsTrackingUri = "";
            expect(component.instance.hasDataEntryErrors()).toEqual(true);

            component.instance.formData.tfsTrackingUri = null;
            expect(component.instance.hasDataEntryErrors()).toEqual(true);
        });

    });
    
    it("setSignalFiltering", () => {
        // arrange
        let component = createComponent();

        // act/assert
        component.instance.radioGroup.model = "Disabled";
        component.instance.setSignalFiltering();

        expect(component.instance.formData.disabledSignalFiltering).toBeTruthy();

        component.instance.radioGroup.model = "Enabled";
        component.instance.setSignalFiltering();

        expect(component.instance.formData.disabledSignalFiltering).toBeFalsy();
    });

    describe("performLinkVariant", () => {
        let component: ComponentInstance<LinkVariantComponent>;
        let variantDataService: DataServiceMock<IVariantDataService>;

        beforeEach(() => {
            component = createComponent();
            variantDataService = spec.dataServiceMocks.variantDataService;
            spec.dataServiceMocks.variantDataService.mockAsyncResultOf("createVariantRequest");

            $modalState.getFor("hide").and.stub();
            spyOn(modalData, "onComplete").and.stub();
        });

        it("should fail to create a variant request until the formData is properly filled out", () => {
            // act
            component.instance.linkVariant();

            // assert
            expect(variantDataService.getFor("createVariantRequest")).not.toHaveBeenCalled();

            spec.runDigestCycle();

            expect($modalState.getFor("hide")).not.toHaveBeenCalled();
            expect(modalData.onComplete).not.toHaveBeenCalled();
        });

        it("should succeed if required fields are set and formated correctly", () => {
            // arrange
            component.instance.formData.disabledSignalFiltering = true;
            component.instance.formData.tfsTrackingUri = "https://test.com";

            // act
            component.instance.linkVariant();

            // assert
            expect(variantDataService.getFor("createVariantRequest")).toHaveBeenCalledWith({
                id: null,
                ownerId: modalData.ownerId,
                ownerName: null,
                requestedVariants: _.map(modalData.variants, v => {
                    return {
                        variantId: v.id,
                        variantName: null,
                        disabledSignalFiltering: true,
                        tfsTrackingUris: ["https://test.com"],
                        variantState: null
                    };
                }),
                variantRelationships: assetGroups.map(ag => {
                    return {
                        assetGroupId: ag.id,
                        assetGroupQualifier: ag.qualifier
                    };
                }),
                trackingDetails: null
            });

            spec.runDigestCycle();

            expect($modalState.getFor("hide")).toHaveBeenCalledWith("^");
            expect(modalData.onComplete).toHaveBeenCalled();
        });

        it("should succeed even if variantExpiryDate is not set", () => {
            // arrange
            component.instance.formData.disabledSignalFiltering = true;
            component.instance.formData.tfsTrackingUri = "https://test.com";

            // act
            component.instance.linkVariant();

            // assert
            expect(variantDataService.getFor("createVariantRequest")).toHaveBeenCalledWith({
                id: null,
                ownerId: modalData.ownerId,
                ownerName: null,
                requestedVariants: _.map(modalData.variants, v => {
                    return {
                        variantId: v.id,
                        variantName: null,
                        disabledSignalFiltering: true,
                        variantExpiryDate: null,
                        tfsTrackingUris: ["https://test.com"],
                        variantState: null
                    };
                }),
                variantRelationships: assetGroups.map(ag => {
                    return {
                        assetGroupId: ag.id,
                        assetGroupQualifier: ag.qualifier
                    };
                }),
                trackingDetails: null
            });

            spec.runDigestCycle();

            expect($modalState.getFor("hide")).toHaveBeenCalledWith("^");
            expect(modalData.onComplete).toHaveBeenCalled();
        });

    });

    function createComponent(): ComponentInstance<LinkVariantComponent> {
        return spec.createComponent<LinkVariantComponent>({
            markup: `<pcd-link-variant></pcd-link-variant>`
        });
    }
});
