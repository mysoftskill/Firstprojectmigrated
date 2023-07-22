import { TestSpec, ComponentInstance, SpyCache } from "../../../shared-tests/spec.base";
import { AssetGroupsListComponent, AssetGroupExtended } from "./asset-groups-list.component";
import { VariantState } from "../../../shared/variant/variant-types";

describe("AssetGroupsList", () => {
    let spec: TestSpec;
    let assetGroups: AssetGroupExtended[];

    beforeEach(() => {
        spec = new TestSpec();

        assetGroups = [{
            id: "DataAsset1ID",
            hasPendingVariantRequests: true,
            hasPendingTransferRequest: true,
            qualifier: {
                props: {
                    AssetType: "DataAsset1Type",
                    propName: "propValue"
                }
            },
            checked: false
        }];
    });

    it("sets the properties correctly onInit", () => {
        // arrange
        let title = "Data assets";

        // act
        let component = createComponent({ assetGroups, title });
        component.scope.$digest();

        // assert
        expect(component.instance.assetGroups).toEqual(assetGroups);
        expect(component.instance.title).toEqual(title);
        expect(component.instance.hasDataInventory()).toEqual(true);
    });

    it("hasPendingVariantRequests works", () => {
        // arrange
        // act
        let component = createComponent({ assetGroups });

        // assert
        expect(component.instance.hasPendingVariantRequests({
            id: "DataAsset1ID",
            qualifier: {
                props: {
                    AssetType: "DataAsset1Type",
                    propName: "propValue"
                }
            }
        })).toBeFalsy();

        expect(component.instance.hasPendingVariantRequests({
            id: "DataAsset1ID",
            hasPendingVariantRequests: true,
            qualifier: {
                props: {
                    AssetType: "DataAsset1Type",
                    propName: "propValue"
                }
            }
        })).toBeTruthy();
    });

    it("hasPendingTransferRequests works", () => {
        // act
        let component = createComponent({ assetGroups });

        // assert
        expect(component.instance.hasPendingTransferRequest({
            id: "DataAsset1ID",
            qualifier: {
                props: {
                    AssetType: "DataAsset1Type",
                    propName: "propValue"
                }
            }
        })).toBeFalsy();

        expect(component.instance.hasPendingTransferRequest({
            id: "DataAsset1ID",
            hasPendingTransferRequest: true,
            qualifier: {
                props: {
                    AssetType: "DataAsset1Type",
                    propName: "propValue"
                }
            }
        })).toBeTruthy();
    });

    it("hasVariant works", () => {
        // act
        let component = createComponent({ assetGroups });

        // assert
        expect(component.instance.hasVariant({
            id: "DataAsset1ID",
            qualifier: {
                props: {
                    AssetType: "DataAsset1Type",
                    propName: "propValue"
                }
            }
        })).toBeFalsy();

        expect(component.instance.hasVariant({
            id: "DataAsset1ID",
            variants: [{
                variantId: "var2",
                variantName: "MockVariant2",
                tfsTrackingUris: [],
                disabledSignalFiltering: false,
                variantState: VariantState.approved
            }],
            qualifier: {
                props: {
                    AssetType: "DataAsset1Type",
                    propName: "propValue"
                }
            }
        })).toBeTruthy();
    });

    function createComponent(data: any): ComponentInstance<AssetGroupsListComponent> {
        return spec.createComponent<AssetGroupsListComponent>({
            markup: `<pcd-asset-groups-list pcd-asset-groups=assetGroups pcd-title="{{title}}"></pcd-asset-groups-list>`,
            data
        });
    }
});
