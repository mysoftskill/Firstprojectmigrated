import * as Pdms from "../../../shared/pdms/pdms-types";
import { TestSpec, ComponentInstance, SpyCache } from "../../../shared-tests/spec.base";
import { IPcdErrorService } from "../../../shared/pcd-error.service";
import { IAngularFailureResponse, JQueryXHRWithErrorResponse } from "../../../shared/ajax.service";
import { IOwnerIdContextService } from "../../../shared/owner-id-context.service";
import { DataAssetPickerComponent } from "./storage-picker.component";

describe("Storage picker", () => {
    let spec: TestSpec;
    let pcdErrorServiceMock: SpyCache<IPcdErrorService>;
    let ownerIdContextServiceMock: SpyCache<IOwnerIdContextService>;
    let componentParentContext: __ComponentParentContext;
    let saveAssetGroupQualifierMock: jasmine.Spy;

    beforeEach(() => {
        spec = new TestSpec();
        inject((_pcdErrorService_: IPcdErrorService,
                _ownerIdContextService_: IOwnerIdContextService) => {

            pcdErrorServiceMock = new SpyCache(_pcdErrorService_);
            ownerIdContextServiceMock = new SpyCache(_ownerIdContextService_);
        });

        let assetType: Pdms.AssetType[] = [{
            id: "AssetType1ID",
            label: "AssetType1Label",
            props: [{
                id: "Prop1ID",
                label: "Prop1Label",
                description: "Prop1Desc",
                required: false
            }]
        }, {
            id: "AssetType2ID",
            label: "AssetType2Label",
            props: [{
                id: "Prop2ID",
                label: "Prop2Label",
                description: "Prop2Desc",
                required: false
            }]
        }];
        spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getAssetTypeMetadata", assetType);

        componentParentContext = new __ComponentParentContext(spec.$promises);
        saveAssetGroupQualifierMock = spyOn(componentParentContext, "saveAssetGroupQualifier").and.callThrough();
    });

    it("sets the assetTypeMap correctly onInit", () => {
        // act
        let component = createComponent();

        // assert
        expect(component.instance.model.assetTypeMap).toEqual({
            AssetType1ID: {
                id: "AssetType1ID",
                label: "AssetType1Label",
                props: [{
                    id: "Prop1ID",
                    label: "Prop1Label",
                    description: "Prop1Desc",
                    required: false
                }]
            },
            AssetType2ID: {
                id: "AssetType2ID",
                label: "AssetType2Label",
                props: [{
                    id: "Prop2ID",
                    label: "Prop2Label",
                    description: "Prop2Desc",
                    required: false
                }]
            }
        });
        expect(component.instance.model.mode).toEqual("preview");
    });

    it("switches mode to 'save' when Preview button is clicked and data service returns valid data assets", () => {
        // arrange
        let response: Pdms.GetDataAssetsByAssetGroupQualifierResponse = {
            dataAssets: [{
                id: "DataAsset1ID",
                qualifier: {
                    props: {
                        AssetType: "DataAsset1Type",
                        prop1Name: "prop1Value",
                        prop2Name: "prop2Value"
                    }
                }
            }, {
                id: "DataAsset2ID",
                qualifier: {
                    props: {
                        AssetType: "DataAsset2Type",
                        prop1Name: "prop1Value",
                        prop2Name: "prop2Value"
                    }
                }
            }],
            dataGridSearch: {
                search: "https://datagrid.microsoft.com",
                searchNext: "https://datagrid.microsoft.com/next"
            }
        };
        spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getDataAssetsByAssetGroupQualifier", response);
        let component = createComponent();

        // act
        component.instance.previewClicked();
        spec.runDigestCycle();

        // assert
        expect(component.instance.dataGridSearch).toEqual(null);
        expect(component.instance.model.previewDataAssets).toEqual(response.dataAssets);
        expect(component.instance.model.mode).toEqual("save");
        expect(component.instance.previewWarning).toEqual("none");
        expect(component.instance.dataAssetsPreviewCaption).toBeTruthy();
        expect(component.instance.noDataAssetsInPreviewLabel).toBeTruthy();
    });

    it("stays in 'preview' mode if data service does not return any data assets on preview", () => {
        // arrange
        let response: Pdms.GetDataAssetsByAssetGroupQualifierResponse = {
            dataAssets: [],
            dataGridSearch: {
                search: "https://datagrid.microsoft.com",
                searchNext: "https://datagrid.microsoft.com/next"
            }
        };
        spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getDataAssetsByAssetGroupQualifier", response);
        let component = createComponent();

        // act
        component.instance.previewClicked();
        spec.runDigestCycle();

        // assert
        expect(component.instance.dataGridSearch).toEqual(response.dataGridSearch);
        expect(component.instance.model.previewDataAssets.length).toEqual(0);
        expect(component.instance.model.mode).toEqual("preview");
        expect(component.instance.previewWarning).toEqual("custom1");
        expect(component.instance.dataAssetsPreviewCaption).toBeFalsy();
    });

    it("shows the alt save button with correct label when Cosmos Structured stream is added", () => {
        // arrange/act
        spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getAssetTypeMetadata", [{
            id: "CosmosStructuredStream",
            label: "AssetType1Label",
            props: [{
                id: "Prop1ID",
                label: "Prop1Label",
                description: "Prop1Desc",
                required: false
            }]
        }]);

        let component = createComponent();
        component.instance.model.mode = "save";

        // assert
        expect(component.instance.altSaveButtonLabel).toEqual("Add only selected stream");
        expect(component.instance.showAltSaveButton()).toBeTruthy();

        expect(component.instance.saveButtonLabel).toEqual("Add structured and unstructured streams");
    });

    it("shows the alt save button with correct label when Cosmos Unstructured stream is added", () => {
        // arrange/act
        spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getAssetTypeMetadata", [{
            id: "CosmosUnstructuredStream",
            label: "AssetType1Label",
            props: [{
                id: "Prop1ID",
                label: "Prop1Label",
                description: "Prop1Desc",
                required: false
            }]
        }]);

        let component = createComponent();
        component.instance.model.mode = "save";

        // assert
        expect(component.instance.altSaveButtonLabel).toEqual("Add only selected stream");
        expect(component.instance.showAltSaveButton()).toBeTruthy();

        expect(component.instance.saveButtonLabel).toEqual("Add structured and unstructured streams");
    });

    it("does not show alt save button when non-Cosmos asset is added", () => {
        // arrange/act
        let component = createComponent();
        component.instance.model.mode = "save";

        // assert
        expect(component.instance.showAltSaveButton()).toBeFalsy();
        expect(component.instance.saveButtonLabel).toEqual("Add");
    });

    it("saves asset group qualifier once for non-Cosmos assets when save is clicked", () => {
        // arrange
        let qualifier = "{'props':{'AssetType':'AzureDocumentDB'}}";
        let component = createComponentWithOnSaveCallback(qualifier);
        component.instance.model.mode = "save";

        // act
        component.instance.onSaveClick();
        component.scope.$digest();

        // assert
        expect(saveAssetGroupQualifierMock).toHaveBeenCalledTimes(1);
        expect(saveAssetGroupQualifierMock).toHaveBeenCalledWith({
            props: {
                AssetType: "AzureDocumentDB"
            }
        });
    });

    it("saves asset group qualifier twice for Cosmos assets when save is clicked", () => {
        // arrange
        let qualifier = "{'props':{'AssetType':'CosmosStructuredStream'}}";
        let component = createComponentWithOnSaveCallback(qualifier);
        component.instance.model.mode = "save";

        // act
        component.instance.onSaveClick();
        component.scope.$digest();

        // assert
        expect(saveAssetGroupQualifierMock).toHaveBeenCalledTimes(2);
        expect(saveAssetGroupQualifierMock.calls.allArgs()).toEqual([[{
            props: {
                AssetType: "CosmosStructuredStream"
            }
        }], [{
            props: {
                AssetType: "CosmosUnstructuredStream"
            }
        }]]);
    });

    it("saves asset group qualifier once for Cosmos assets when alt-save is clicked", () => {
        // arrange
        let qualifier = "{'props':{'AssetType':'CosmosStructuredStream'}}";
        let component = createComponentWithOnSaveCallback(qualifier);
        component.instance.model.mode = "save";

        // act
        component.instance.onAltSaveClick();
        component.scope.$digest();

        // assert
        expect(saveAssetGroupQualifierMock).toHaveBeenCalledTimes(1);
        expect(saveAssetGroupQualifierMock).toHaveBeenCalledWith({
            props: {
                AssetType: "CosmosStructuredStream"
            }
        });
        expect(saveAssetGroupQualifierMock).not.toHaveBeenCalledWith({
            props: {
                AssetType: "CosmosUnstructuredStream"
            }
        });
    });

    describe("qualifier bound", () => {
        it("sets the correct asset group qualifier", () => {
            // arrange
            let qualifier = "{'props':{'AssetType':'AzureDocumentDB','AccountName':'AnyAccountName','DatabaseName':'OptionalDBName','CollectionName':'OptionalCollectionName'}}";

            // act
            let component = createComponentWithQualifier(qualifier);
            component.scope.$digest();

            // assert
            expect(component.instance.model.qualifier.props.AssetType).toBe("AzureDocumentDB");
            expect(component.instance.model.qualifier.props["AccountName"]).toBe("AnyAccountName");
            expect(component.instance.model.qualifier.props["DatabaseName"]).toBe("OptionalDBName");
            expect(component.instance.model.qualifier.props["CollectionName"]).toBe("OptionalCollectionName");
        });

        it("sets the correct qualifier properties", () => {
            // arrange
            let qualifier = "{'props':{'AssetType':'AssetType1ID','Prop1ID':'Optional1'}}";

            // act
            let component = createComponentWithQualifier(qualifier);
            component.scope.$digest();

            // assert
            expect(component.instance.model.qualifierProperties[0].meta.id).toBe("Prop1ID");
            expect(component.instance.model.qualifierProperties[0].value).toBe("Optional1");
        });

        it("sets the correct hint", () => {
            // arrange
            let qualifier = "{'props':{'AssetType':'AzureDocumentDB','AccountName':'AnyAccountName','DatabaseName':'OptionalDBName','CollectionName':'OptionalCollectionName'}}";

            // act
            let component = createComponentWithQualifier(qualifier);
            component.scope.$digest();

            // assert
            expect(component.instance.model.qualifier.props.AssetType).toBe("AzureDocumentDB");
            expect(component.instance.model.hint.placeholders["AccountName"]).not.toBeNull();
            expect(component.instance.model.hint.placeholders["DatabaseName"]).not.toBeNull();
            expect(component.instance.model.hint.placeholders["CollectionName"]).not.toBeNull();
        });
    });

    describe("error handler", () => {
        it("shows simple warning, if asset already exists, ownerId is provided and is not matching active owner ID", () => {
            // arrange
            let errorPayload: Partial<JQueryXHRWithErrorResponse> = {
                responseJSON: {
                    error: "alreadyExists",
                    data: {
                        ownerId: "testOwnerId"
                    }
                }
            };
            let response: Partial<IAngularFailureResponse> = {
                jqXHR: errorPayload
            };
            spec.dataServiceMocks.pdmsDataService.mockFailureOf("getDataAssetsByAssetGroupQualifier", response);

            pcdErrorServiceMock.failIfCalled("setError");
            spec.$state.spies.getFor("href").and.returnValue("https://example.com/editor-link");

            // Crucial for this test: active owner's ID doesn't match the one in the error payload.
            ownerIdContextServiceMock.getFor("getActiveOwnerId").and.returnValue("activeOwnerId");

            let component = createComponent();

            // act
            component.instance.previewClicked();
            spec.runDigestCycle();

            // assert
            expect(component.instance.previewWarning).toBe("simple");
            expect(component.instance.previewWarningText).toBeTruthy();
            expect(component.instance.previewWarningLinkText).toBeTruthy();
            expect(component.instance.previewWarningLink).toBe("https://example.com/editor-link");
            expect(spec.$state.spies.getFor("href")).toHaveBeenCalledWith("data-owners.view", { ownerId: "testOwnerId" });
        });

        it("shows regular error, if asset already exists, ownerId is provided and is matching active owner ID", () => {
            // arrange
            let errorPayload: Partial<JQueryXHRWithErrorResponse> = {
                responseJSON: {
                    error: "alreadyExists",
                    data: {
                        ownerId: "testOwnerId"
                    }
                }
            };
            let response: Partial<IAngularFailureResponse> = {
                jqXHR: errorPayload
            };
            spec.dataServiceMocks.pdmsDataService.mockFailureOf("getDataAssetsByAssetGroupQualifier", response);

            pcdErrorServiceMock.getFor("setError").and.callThrough();
            spec.$state.spies.failIfCalled("href");

            // Crucial for this test: active owner's ID matches the one in the error payload.
            ownerIdContextServiceMock.getFor("getActiveOwnerId").and.returnValue("testOwnerId");

            let component = createComponent();

            // act
            component.instance.previewClicked();
            spec.runDigestCycle();

            // assert
            expect(component.instance.previewWarning).toBe("none");
            expect(pcdErrorServiceMock.getFor("setError")).toHaveBeenCalledWith(response, component.instance.errorCategory, jasmine.any(Object));
        });

        it("sets error on qualifier property, if invalidInput error occurred and property ID is known", () => {
            // arrange
            let errorPayload: Partial<JQueryXHRWithErrorResponse> = {
                responseJSON: {
                    error: "invalidInput",
                    data: {
                        // Crucial for this test: target must match one of the known properties.
                        target: "knownProperty"
                    }
                }
            };
            let response: Partial<IAngularFailureResponse> = {
                jqXHR: errorPayload
            };
            spec.dataServiceMocks.pdmsDataService.mockFailureOf("getDataAssetsByAssetGroupQualifier", response);

            pcdErrorServiceMock.getFor("setErrorForId").and.callThrough();
            pcdErrorServiceMock.failIfCalled("setError");

            let component = createComponent();
            component.instance.model.qualifierProperties = [{
                meta: {
                    id: "knownProperty",
                    label: "known property label",
                    description: "known property label"
                },
                value: ""
            }];

            // act
            component.instance.previewClicked();
            spec.runDigestCycle();

            // assert
            expect(pcdErrorServiceMock.getFor("setErrorForId")).toHaveBeenCalledWith(component.instance.getErrorIdForProperty("knownProperty"), jasmine.any(String));
        });

        it("shows regular error, if invalidInput error occurred and property ID is unknown", () => {
            // arrange
            let errorPayload: Partial<JQueryXHRWithErrorResponse> = {
                responseJSON: {
                    error: "invalidInput",
                    data: {
                        // Crucial for this test: target should not match any of the known properties.
                        target: "unknownProperty"
                    }
                }
            };
            let response: Partial<IAngularFailureResponse> = {
                jqXHR: errorPayload
            };
            spec.dataServiceMocks.pdmsDataService.mockFailureOf("getDataAssetsByAssetGroupQualifier", response);

            pcdErrorServiceMock.failIfCalled("setErrorForId");
            pcdErrorServiceMock.getFor("setError").and.callThrough();

            let component = createComponent();
            component.instance.model.qualifierProperties = [{
                meta: {
                    id: "knownProperty",
                    label: "known property label",
                    description: "known property label"
                },
                value: ""
            }];

            // act
            component.instance.previewClicked();
            spec.runDigestCycle();

            // assert
            expect(pcdErrorServiceMock.getFor("setError")).toHaveBeenCalledWith(response, component.instance.errorCategory, jasmine.any(Object));
        });
    });

    function createComponent(): ComponentInstance<DataAssetPickerComponent> {
        return spec.createComponent<DataAssetPickerComponent>({
            markup: `<pcd-storage-picker></pcd-storage-picker>`
        });
    }

    function createComponentWithOnSaveCallback(qualifier: string): ComponentInstance<DataAssetPickerComponent> {
        return spec.createComponent<DataAssetPickerComponent>({
            markup: `<pcd-storage-picker pcd-asset-group-qualifier="${qualifier}" pcd-on-save="saveQualifier(qualifier)"></pcd-storage-picker>`,
            data: {
                saveQualifier: (assetGroupQualifierArg: Pdms.AssetGroupQualifier) => {
                    return componentParentContext.saveAssetGroupQualifier(assetGroupQualifierArg);
                }
            }
        });
    }

    function createComponentWithQualifier(qualifier: string): ComponentInstance<DataAssetPickerComponent> {
        return spec.createComponent<DataAssetPickerComponent>({
            markup: `<pcd-storage-picker pcd-asset-group-qualifier="${qualifier}"></pcd-storage-picker>`
        });
    }
});

class __ComponentParentContext {
    constructor(
        private readonly $q: ng.IQService) { }

    public saveAssetGroupQualifier(qualifier: Pdms.AssetGroupQualifier): ng.IPromise<any> {
        return this.$q.resolve();
    }
}
