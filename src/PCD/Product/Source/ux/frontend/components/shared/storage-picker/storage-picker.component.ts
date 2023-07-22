import * as angular from "angular";

import template = require("./storage-picker.html!text");
import { Component, Inject } from "../../../module/app.module";
import { IOwnerIdContextService } from "../../../shared/owner-id-context.service";
import { IAngularFailureResponse } from "../../../shared/ajax.service";
import { ErrorCodeHelper } from "../../shared/utilities/error-code-helper";
import { IStringFormatFilter } from "../../../shared/filters/string-format.filter";
import { IPcdErrorService, DefaultErrorMessages, PcdErrorOverrides } from "../../../shared/pcd-error.service";
import { DataAssetHelper } from "../../../shared/data-asset-helper";
import { WarningStyle } from "./data-assets-view.component";

import * as Pdms from "../../../shared/pdms/pdms-types";

type OnSaveArgs = {
    qualifier: Pdms.AssetGroupQualifier;
};

interface AssetGroupQualifierProperty {
    meta: Pdms.AssetTypeProperty;
    value: string;
}

const useCmsHere_ButtonSave = "Add";
const useCmsHere_ButtonSaveCosmos = "Add structured and unstructured streams";
const useCmsHere_ButtonAltSaveCosmos = "Add only selected stream";
const useCmsHere_ButtonUpdate = "Update";
const useCmsHere_ContactSupportLinkText = "Help";
const useCmsHere_AlreadyExistsWarningLinkText = "Which team?";
const useCmsHere_PreviewCaption = "Data assets found in DataGrid";
const useCmsHere_PreviewCaptionWithNItems = "Showing first {0} data assets from DataGrid. Please verify and click the {1} button below.";
const useCmsHere_PreviewCallToActionLabel = "Please provide asset group qualifier information and click the Preview button.";
const useCmsHere_NoDataAssetsInPreviewLabel = "No data assets were found matching this criteria in DataGrid.";
const useCmsHere_AssetGroupAlreadyExists = "The data asset you're trying to add already exists.";
const useCmsHere_AssetGroupAlreadyExistsDifferentOwner = "The data asset you're trying to add, has been claimed by a team earlier.";

const Storage_Picker_Error_Overrides: PcdErrorOverrides = {
    overrides: {
        errorMessages: {
            "alreadyExists": useCmsHere_AssetGroupAlreadyExists
        }
    }
};

interface InputHint {
    description: string[];
    placeholders: {
        [key: string]: string;
    };
}

interface InputHints {
    [key: string]: InputHint;
}

const useCmsHere_InputHints: InputHints = {
    "API": {
        description: [
            "Given an API URL, you can identify the corresponding properties by examining the following format string:",
            "<host>/<path>/<method>",
            "An example for this would be:",
            "https://contoso.microsoft.com/api/v1/GET",
            "The host would be https://contoso.microsoft.com, and the path can be empty to indicate the entire API, or any subpath (such as api/v1). Any partial path will cover all assets that have the same path or contain a path that is under the partial path.  The method must be a valid HTTP Request Method."
        ],
        placeholders: {
            "Host": "e.g., https://contoso.microsoft.com",
            "Path": "e.g., /api/v1",
            "Method": "e.g., GET"
        }
    },
    "ApplicationService": {
        description: [
            "Given a service URL, you can identify the corresponding properties by examining the following format string:",
            "https://<host>/<path>",
            "An example for this would be:",
            "https://contoso.microsoft.com/api/v1/users",
            "The host would be contoso.microsoft.com, and the path can be empty to indicate the entire URI, or any subpath (such as api/v1). Any partial path will cover all assets that have the same path or contain a path that is under the partial path."
        ],
        placeholders: {
            "Host": "e.g., contoso.microsoft.com",
            "Path": "e.g., api/v1"
        }
    },
    "AzureBlob": {
        description: [
            "Given an Azure Blob URL, you can identify the corresponding properties by examining the follow format string:",
            "https://<Account Name>.blob.core.windows.net/<Container Name>/<Blob Pattern>",
            "An example for this would be:",
            "https://contoso.blob.core.windows.net/users/reports/2016/11/05/09752394875",
            "The Blob Pattern may be a partial value. For example, you could register /reports and that would cover all blobs under that logical folder."
        ],
        placeholders: {
            "AccountName": "e.g., contoso",
            "ContainerName": "e.g., users",
            "BlobPattern": "e.g., reports"
        }
    },
    "AzureDocumentDB": {
        description: [
            "Given an Azure Document DB URL, you can identify the corresponding properties by examining the follow format string:",
            "<Account Name>/dbs/<Database Name>/colls/<Collection Name>/docs/documentId",
            "An example for this would be:",
            "https://contoso.documents.azure.com/dbs/users/colls/reports/docs/09752394875"
        ],
        placeholders: {
            "AccountName": "e.g., https://contoso.documents.azure.com",
            "DatabaseName": "e.g., users",
            "CollectionName": "e.g., reports"
        }
    },
    "AzureSql": {
        description: [
            "Given an Azure SQL connection string, you can identify the corresponding properties by examining the follow format string:",
            "Server=tcp:<Server Host Name>;Database=<Database Name>;Trusted_Connection=False;Encrypt=True;",
            "An example for this would be:",
            "Server=tcp:contoso.database.windows.net;Database=users;Trusted_Connection=False;Encrypt=True;",
            "Table names are not provided as part of the connection string. You would need to identify those manually by examining your storage schema. However, it is recommended to register at the server name level."
        ],
        placeholders: {
            "ServerName": "e.g., contoso.database.windows.net",
            "DatabaseName": "e.g., users",
            "TableName": "e.g., reports"
        }
    },
    "AzureTable": {
        description: [
            "Given an Azure Table URL, you can identify the corresponding properties by examining the follow format string:",
            "https://<Account Name>.table.core.windows.net/<Table Name>",
            "An example for this would be:",
            "https://contoso.table.core.windows.net/userReports"
        ],
        placeholders: {
            "AccountName": "e.g., contoso",
            "TableName": "e.g., userReports"
        }
    },
    "CosmosStructuredStream": {
        description: [
            "Given a Cosmos Structure Stream URL, you can identify the corresponding properties by examining the follow format string:",
            "https://<Physical Cluster>.osdinfra.net/cosmos/<Virtual Cluster>/<Relative Path>",
            "An example for this would be:",
            "https://cosmos15.osdinfra.net/cosmos/contoso/local/processed/users/reports/2016/11/05/09752394875.ss",
            "The Relative Path may be a partial value. For example, you could register /local/processed and that would cover all stream sets under that folder. The relative path must be the physical path for the stream set and cannot start with /my/, /public/, /shares/ or /users/."
        ],
        placeholders: {
            "PhysicalCluster": "e.g., cosmos15",
            "VirtualCluster": "e.g., contoso",
            "RelativePath": "e.g., /local/processed/users/reports/2016/11/05/09752394875.ss"
        }
    },
    "CosmosUnstructuredStream": {
        description: [
            "Given a Cosmos Unstructured Stream URL, you can identify the corresponding properties by examining the follow format string:",
            "https://<Physical Cluster>.osdinfra.net/cosmos/<Virtual Cluster>/<Relative Path>",
            "An example for this would be:",
            "https://cosmos15.osdinfra.net/cosmos/contoso/local/raw/users/reports/2016/11/05/09752394875.tsv",
            "The Relative Path may be a partial value. For example, you could register /local/processed and that would cover all stream sets under that folder. The relative path must be the physical path for the stream set and cannot start with /my/, /public/, /shares/ or /users/."
        ],
        placeholders: {
            "PhysicalCluster": "e.g., cosmos15",
            "VirtualCluster": "e.g., contoso",
            "RelativePath": "e.g., /local/processed/users/reports/2016/11/05/09752394875.ss"
        }
    },
    "File": {
        description: [
            "Given a File URL, you can identify the corresponding properties by examining the follow format string:",
            "\\\\<Server Path>\\<File Name>",
            "An example for this would be:",
            "\\\\contoso-server01\\e$\\users\\reports\\2016\\11\\05\\09752394875.tsv",
            "The Server Path may be a partial value. For example, you could register \\\\contoso - server01 and that would cover all files under that server."
        ],
        placeholders: {
            "ServerPath": "e.g., contoso-server01",
            "FileName": "e.g., e$\\users\\reports\\2016\\11\\05\\09752394875.tsv"
        }
    },
    "PlatformService": {
        description: [
            "Given a service URL, you can identify the corresponding properties by examining the following format string:",
            "https://<host>/<path>",
            "An example for this would be:",
            "https://contoso.microsoft.com/api/v1/users",
            "The host would be contoso.microsoft.com, and the path can be empty to indicate the entire URI, or any subpath (such as api/v1). Any partial path will cover all assets that have the same path or contain a path that is under the partial path."
        ],
        placeholders: {
            "Host": "e.g., contoso.microsoft.com",
            "Path": "e.g., api/v1"
        }
    },
    "SqlServer": {
        description: [
            "Given a SQL Server connection string, you can identify the corresponding properties by examining the follow format string:",
            "Server=tcp:<Server Name>;Database=<Database Name>;Trusted_Connection=False;Encrypt=True;",
            "An example for this would be:",
            "Server=tcp:myserver;Database=users;Trusted_Connection=False;Encrypt=True;",
            "Table names are not provided as part of the connection string. You would need to identify those manually by examining your storage schema. However, it is recommended to register at the server name level.",
        ],
        placeholders: {
            "ServerName": "e.g., myserver",
            "DatabaseName": "e.g., users",
            "TableName": "e.g., reports"
        }
    }
};

const actionTypeMap = {
    add: useCmsHere_ButtonSave,
    update: useCmsHere_ButtonUpdate
};

const previewAssetsPagingSize = 10;

const DefaultAssetTypeId = "CosmosStructuredStream";

export const ErrorCategory = "storage-picker";

@Component({
    name: "pcdStoragePicker",
    options: {
        template,
        bindings: {
            assetGroupQualifier: "<pcdAssetGroupQualifier",
            onSave: "&pcdOnSave",
            actionType: "@"
        },
        transclude: {
            moreControls: "?pcdMoreControls"
        }
    }
})
@Inject("pdmsDataService", "$state", "$timeout", "$element", "pcdErrorService", "stringFormatFilter", "ownerIdContextService")
export class DataAssetPickerComponent implements ng.IComponentController {
    /** 
     * Input: callback that occurs when asset group qualifier needs to be saved. 
     **/
    public onSave: (args: OnSaveArgs) => ng.IPromise<any>;
    public actionType: string;

    public assetGroupQualifier: Pdms.AssetGroupQualifier;
    public saveButtonLabel = useCmsHere_ButtonSave;
    public altSaveButtonLabel = useCmsHere_ButtonSave;
    public errorCategory = ErrorCategory;
    public previewWarning: WarningStyle;

    public dataAssetsPreviewCaption = "";
    public noDataAssetsInPreviewLabel = useCmsHere_PreviewCallToActionLabel;
    public previewWarningText = "";
    public previewWarningLink = "";
    public previewWarningLinkText = "";
    public contactSupportLinkText = useCmsHere_ContactSupportLinkText;

    public dataGridSearch: Pdms.DataGridSearch;

    public model: {
        qualifier: Pdms.AssetGroupQualifier;
        previewDataAssets: Pdms.DataAsset[];
        assetTypes: Pdms.AssetType[];
        assetTypeMap: {
            [id: string]: Pdms.AssetType
        },
        qualifierProperties: AssetGroupQualifierProperty[];
        mode: "preview" | "save";
        hint: InputHint;
    };

    constructor(
        private readonly pdmsData: Pdms.IPdmsDataService,
        private readonly $state: ng.ui.IStateService,
        private readonly $timeout: ng.ITimeoutService,
        private readonly $element: ng.IRootElementService,
        private readonly pcdError: IPcdErrorService,
        private readonly formatFilter: IStringFormatFilter,
        private readonly ownerIdContextService: IOwnerIdContextService) { }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("initializeStoragePickerComponent")
    public $onInit(): ng.IPromise<any> {
        this.pcdError.resetErrorsForCategory(this.errorCategory);

        this.model = {
            qualifier: null,
            previewDataAssets: [],
            assetTypes: [],
            assetTypeMap: {},
            qualifierProperties: [],
            mode: "preview",
            hint: null,
        };

        return this.pdmsData.getAssetTypeMetadata()
            .then((assetTypes: Pdms.AssetType[]) => {
                this.model.assetTypes = assetTypes;

                assetTypes.forEach(assetType => {
                    this.model.assetTypeMap[assetType.id] = assetType;
                });

                if (this.assetGroupQualifier) {
                    this.saveButtonLabel = useCmsHere_ButtonUpdate;
                    this.model.qualifier = angular.copy(this.assetGroupQualifier);
                    this.updateInputHint();
                    this.resetStoragePicker();
                } else {
                    this.selectAssetTypeDefault();
                }
            });
    }

    public enablePreviewButton(): boolean {
        return this.isInputValid() && this.model.mode === "preview";
    }

    public enableSaveButton(): boolean {
        return this.isInputValid() && this.model.mode === "save";
    }

    public onAssetTypeChange(): void {
        //  Clear up the rest of props otherwise they will linger in the map.
        this.model.qualifier.props = {
            AssetType: this.model.qualifier.props.AssetType,
        };

        this.resetStoragePicker();
    }

    public resetStoragePicker(): void {
        this.pcdError.resetErrorsForCategory(this.errorCategory);

        this.resetPreviewBox();
        this.updateInputHint();

        let useExistingProps = this.assetGroupQualifier && this.assetGroupQualifier.props.AssetType === this.model.qualifier.props.AssetType;
        this.model.qualifierProperties = this.model.assetTypeMap[this.model.qualifier.props.AssetType].props.map(typeProperty => {
            return <AssetGroupQualifierProperty>{
                meta: typeProperty,
                value: (useExistingProps && this.assetGroupQualifier.props[typeProperty.id]) || ""
            };
        });

        this.setModeToPreview();
        this.resetSaveButtonLabels();
        this.focusFirstProperty();
    }

    public qualifierPropertyChanged(prop: AssetGroupQualifierProperty): void {
        this.model.qualifier.props[prop.meta.id] = prop.value;

        this.resetPreviewBox();
        this.setModeToPreview();
    }

    public setModeToPreview(): void {
        this.model.mode = "preview";
    }

    public previewClicked(): void {
        this.resetErrors();

        this.previewAssetGroupQualifier()
            .then((response: Pdms.GetDataAssetsByAssetGroupQualifierResponse) => {
                let dataAssets = response.dataAssets;

                if (!dataAssets || !dataAssets.length) {
                    this.previewWarning = "custom1";
                    this.dataGridSearch = response.dataGridSearch;

                    this.dataAssetsPreviewCaption = "";
                    this.noDataAssetsInPreviewLabel = useCmsHere_NoDataAssetsInPreviewLabel;

                    this.model.previewDataAssets = [];
                } else {
                    this.previewWarning = "none";
                    this.dataGridSearch = null;

                    this.model.previewDataAssets = dataAssets;
                    if (dataAssets.length >= previewAssetsPagingSize) {
                        this.dataAssetsPreviewCaption = this.formatFilter(
                            useCmsHere_PreviewCaptionWithNItems,
                            [previewAssetsPagingSize, actionTypeMap[this.actionType] || ""]
                        );
                    } else {
                        this.dataAssetsPreviewCaption = useCmsHere_PreviewCaption;
                    }

                    this.model.mode = "save";
                    this.$timeout().then(() => angular.element("#storage-picker-save").focus());
                }
            })
            .catch((e: IAngularFailureResponse) => {
                this.handleErrors("preview", e);
            });
    }

    public getErrorIdForProperty(propertyId: string): string {
        return `${this.errorCategory}.prop.${propertyId}`.toLowerCase();
    }

    public showAltSaveButton(): boolean {
        return this.isInputValid() &&
               this.model.mode === "save" &&
               DataAssetHelper.isCosmosAssetTypeId(this.model.qualifier.props.AssetType);
    }

    public onSaveClick(): ng.IPromise<any> {
        return this.saveAssetGroupQualifier(true /* saveWithAltStream */);
    }

    public onAltSaveClick(): ng.IPromise<any> {
        return this.saveAssetGroupQualifier(false /* saveWithAltStream */);
    }

    public getDataGridLink(): string {
        /// returning link to search across all tenants
        return `${this.dataGridSearch.search}&teamPath=0`;
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("saveAssetGroupQualifier")
    public saveAssetGroupQualifier(saveWithAltStream: boolean): ng.IPromise<any> {
        this.resetErrors();

        return this.performSave(false /* flipCosmosAssetType */)
            .then(() => {
                if (saveWithAltStream && DataAssetHelper.isCosmosAssetTypeId(this.model.qualifier.props.AssetType)) {
                    return this.performSave(true /* flipCosmosAssetType */)
                               .then(() => this.postSaveReset())
                               .catch((e: IAngularFailureResponse) => this.handleErrors("save", e));
                }

                this.postSaveReset();
            })
            .catch((e: IAngularFailureResponse) => this.handleErrors("save", e));
    }

    public postSaveReset(): void {
        //  Reset model qualifier and storage picker. This effectively sets mode back to "preview".
        this.model.qualifier = { props: { AssetType: this.model.qualifier.props.AssetType } };
        this.resetStoragePicker();
    }

    private getQualifierFromModel(flipCosmosAssetType: boolean): Pdms.AssetGroupQualifier {
        let newAssetType = this.model.qualifier.props.AssetType;

        //  If adding Cosmos Structured/Unstructured Stream data asset, flip asset type.
        if (flipCosmosAssetType && DataAssetHelper.isCosmosAssetTypeId(this.model.qualifier.props.AssetType)) {
            newAssetType = DataAssetHelper.isCosmosUnstructuredStreamAssetTypeId(this.model.qualifier.props.AssetType) ?
                "CosmosStructuredStream" : "CosmosUnstructuredStream";
        }

        let newQualifier: Pdms.AssetGroupQualifier = {
            props: {
                AssetType: newAssetType
            }
        };

        //  Copy all non-falsy properties from model to a new object.
        newQualifier = this.model.qualifierProperties
            .reduce((qualifier, prop) => {
                if (this.model.qualifier.props[prop.meta.id]) {
                    qualifier.props[prop.meta.id] = this.model.qualifier.props[prop.meta.id];
                }
                return qualifier;
            }, newQualifier);

        return newQualifier;
    }

    private resetSaveButtonLabels(): void {
        //  If asset type is Cosmos stream, change save button label to reflect both streams.
        if (DataAssetHelper.isCosmosAssetTypeId(this.model.qualifier.props.AssetType)) {
            this.saveButtonLabel = useCmsHere_ButtonSaveCosmos;
            this.altSaveButtonLabel = useCmsHere_ButtonAltSaveCosmos;
        } else {
            this.saveButtonLabel = useCmsHere_ButtonSave;
        }
    }

    private resetPreviewBox(): void {
        this.previewWarning = "none";
        this.dataAssetsPreviewCaption = "";
        this.noDataAssetsInPreviewLabel = useCmsHere_PreviewCallToActionLabel;
        this.model.previewDataAssets = [];
    }

    private performSave(flipCosmosAssetType: boolean): ng.IPromise<any> {
        return this.onSave({
            qualifier: this.getQualifierFromModel(flipCosmosAssetType)
        });
    }

    private updateInputHint(): void {
        if (this.model && this.model.qualifier && this.model.qualifier.props && this.model.qualifier.props.AssetType) {
            this.model.hint = useCmsHere_InputHints[this.model.qualifier.props.AssetType];
        }
    }

    private focusFirstProperty(): void {
        this.$timeout().then(() => angular.element(".asset-group-qualifier-property", this.$element).eq(0).focus());
    }

    @MeePortal.OneUI.Angular.MonitorOperationProgress("previewAssetGroupQualifier")
    private previewAssetGroupQualifier(): ng.IPromise<Pdms.GetDataAssetsByAssetGroupQualifierResponse> {
        return this.pdmsData.getDataAssetsByAssetGroupQualifier(this.getQualifierFromModel(false /* flipCosmosAssetType */));
    }

    private resetErrors(): void {
        this.pcdError.resetErrorsForCategory(this.errorCategory);
        this.previewWarning = "none";
    }

    private selectAssetTypeDefault(): void {
        if (!this.model || !this.model.assetTypes) {
            return;
        }

        let target = _.find(this.model.assetTypes, at => DefaultAssetTypeId === at.id);
        this.model.qualifier = {
            props: {
                AssetType: (target || this.model.assetTypes[0]).id
            }
        };
        this.onAssetTypeChange();
    }

    private handleErrors(id: string, e: IAngularFailureResponse): void {
        let errorResponse = ErrorCodeHelper.getErrorResponse(e);
        let errorCode = ErrorCodeHelper.getErrorCode(e);

        switch (errorCode) {
            case "invalidInput":
                if (this.model.qualifierProperties.some(prop => prop.meta.id === errorResponse.data["target"])) {
                    this.pcdError.setErrorForId(
                        this.getErrorIdForProperty(errorResponse.data["target"]),
                        errorResponse.data["message"] || DefaultErrorMessages[errorCode]);

                    //  No need to process errors any further.
                    return;
                }
                break;

            case "alreadyExists":
                let ownerId = errorResponse.data["ownerId"];
                if (ownerId && this.ownerIdContextService.getActiveOwnerId() !== ownerId) {
                    this.previewWarningText = useCmsHere_AssetGroupAlreadyExistsDifferentOwner;
                    this.previewWarningLinkText = useCmsHere_AlreadyExistsWarningLinkText;
                    this.previewWarningLink = this.$state.href("data-owners.view", { ownerId });
                    this.previewWarning = "simple";

                    //  No need to process errors any further.
                    return;
                }
                break;
        }

        this.pcdError.setError(e, this.errorCategory, { ...Storage_Picker_Error_Overrides, genericErrorId: id });
    }

    private isInputValid(): boolean {
        return this.model && this.model.qualifierProperties && this.model.qualifierProperties.every(prop => !prop.meta.required || !!prop.value);
    }
}
