import { TestSpec, ComponentInstance } from "../../../shared-tests/spec.base";

import VariantDetailsView from "./variant-details-view.component";

describe("VariantDetailsView", () => {
    let spec: TestSpec;

    beforeEach(() => {
        spec = new TestSpec();

    });

    describe("for isReady", () => {
        it("should be false when all bindings are null", () => {
            // arrange
            let component = createComponent();

            // act/assert
            expect(component.instance.isReady()).toBeFalsy();
        });

        it("should be true when only required bindings are properly defined", () => {
            // arrange
            let component = createComponent();
            component.instance.variantDefinition = {
                id: "",
                name: "",
                approver: "",
                capabilities: [],
                dataTypes: [],
                description: "",
                ownerId: "",
                subjectTypes: []
            };

            component.instance.qualifier = {
                dataGridLink: "",
                props: {
                    AssetType: ""
                }
            };

            // act/assert
            expect(component.instance.isReady()).toBeTruthy();
        });

        it("should be true when all bindings are defined", () => {
            // arrange
            let component = createComponent();
            component.instance.variantDefinition = {
                id: "",
                name: "",
                approver: "",
                capabilities: [],
                dataTypes: [],
                description: "",
                ownerId: "",
                subjectTypes: []
            };

            component.instance.qualifier = {
                dataGridLink: "",
                props: {
                    AssetType: ""
                }
            };

            component.instance.tfsTrackingUris = [];
            component.instance.disabledSignalFiltering = false;

            // act/assert
            expect(component.instance.isReady()).toBeTruthy();
        });
    });

    function createComponent(): ComponentInstance<VariantDetailsView> {
        return spec.createComponent<VariantDetailsView>({
            markup: `<pcd-variant-details-view></pcd-variant-details-view>`
        });
    }
});
