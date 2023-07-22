import { TestSpec, ComponentInstance } from "../../../shared-tests/spec.base";
import { DataAssetsViewComponent } from "./data-assets-view.component";
import * as Pdms from "../../../shared/pdms/pdms-types";

describe("DataAssetsView", () => {
    let spec: TestSpec;

    beforeEach(() => {
        spec = new TestSpec();
    });

    it("sets the properties correctly onInit", () => {
        // arrange
        let dataAssets: Pdms.DataAsset[] = [{
            id: "DataAsset1ID",
            qualifier: {
                props: {
                    AssetType: "DataAsset1Type",
                    propName: "propValue"
                }
            }
        }];
        let component = createComponent(dataAssets);

        // act
        component.scope.$digest();

        // assert
        expect(component.instance.hasDataAssets()).toBe(true);
        expect(component.instance.ngModel).toEqual(dataAssets);
        expect(component.instance.caption).toEqual("Caption text");
        expect(component.instance.noDataAssetsLabel).toEqual("No data assets");
        expect(component.instance.warningStyle).toEqual("simple");
    });

    function createComponent(model: Pdms.DataAsset[]): ComponentInstance<DataAssetsViewComponent> {
        return spec.createComponent<DataAssetsViewComponent>({
            markup: `<pcd-data-assets-view
                          pcd-caption="Caption text"
                          pcd-no-data-assets-label="No data assets"
                          pcd-warning-style="simple"
                          ng-model="model"></pcd-data-assets-view>`,
            data: {
                model
            }
        });
    }
});
