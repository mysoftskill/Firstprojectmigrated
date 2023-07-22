import { Component, Inject } from "../../../module/app.module";
import template = require("./directory-resource-selector.html!text");

import * as SelectorTypes from "./directory-resource-selector-types";
import { IDirectoryResourceSelectorFactory } from "./directory-resource-selector-provider";
import { IPcdErrorService } from "../../../shared/pcd-error.service";
import { SelectorType } from "./directory-resource-selector-types";

const useCmsHere_GenericValidationError = "Please check your input.";
const useCmsHere_DefaultNoSuggestionLabel = "No suggestions";

@Component({
    name: "pcdDirectoryResourceSelector",
    options: {
        template,
        bindings: {
            resourceType: "@pcdResourceType",
            displayLabel: "@pcdSelectorLabel",
            errorId: "@pcdErrorId",
            placeholder: "@?pcdPlaceholder",
            addedResourcePadding: "<?pcdAddedResourcePadding",
            addOnBlur: "<?pcdAddOnBlur",
            addOnEnter: "<?pcdAddOnEnter",
            ngModel: "<",
        },
        require: { ngModelCtrl: "^ngModel" },
    }
})
@Inject("$timeout", "pcdErrorService", "directoryResourceSelectorFactory", "$meeMonitoredOperation", "$meeUtil")
export default class DirectoryResourceSelectorComponent implements ng.IComponentController {
    private ngModelCtrl: ng.INgModelController;
    public resourceSelector: SelectorTypes.IResourceSelector;
    public ngModel: SelectorTypes.DirectoryResourceSelectorExposedData;

    private errorId: string;
    private placeholder: string;
    private addedResourcePadding: boolean;
    private addOnBlur: boolean;
    private addOnEnter: boolean;

    public resourceType: SelectorType;
    public isAutoSuggestEnabled = true;
    public hasFocus = false;
    public displayLabel: string;
    public textBoxContent: string;
    public addedResources: SelectorTypes.AddedResource[] = [];
    public noSuggestionLabel: string;
    public placeholderLabel: string;
    public progressMonitoredOperationName: string;

    constructor(
        private readonly $timeout: ng.ITimeoutService,
        private readonly pcdError: IPcdErrorService,
        private readonly directoryResourceSelectorFactory: IDirectoryResourceSelectorFactory,
        private readonly monitoredOperation: MeePortal.OneUI.Angular.IMonitoredOperation,
        private readonly meeUtil: MeePortal.OneUI.Angular.IMeeUtil,
    ) { }

    public $onInit(): void {
        this.pcdError.resetErrorForId(this.errorId);

        this.resourceSelector = this.directoryResourceSelectorFactory.createInstance(this.resourceType, this.ngModel);
        this.isAutoSuggestEnabled = this.resourceSelector.isAutoSuggestAllowed();

        if (this.addOnBlur && this.isAutoSuggestEnabled) {
            console.warn("DRS should not have addOnBlur and isAutoSuggestEnabled set together.");
        }

        let strings = this.resourceSelector.getDefaultStrings();
        this.noSuggestionLabel = strings.noSuggestionLabel || useCmsHere_DefaultNoSuggestionLabel;
        this.placeholderLabel = this.placeholder || strings.placeholderLabel;
        this.progressMonitoredOperationName = `initializeResources${this.meeUtil.nextUid()}`;

        // Timeout is required so that the digest cycle of angular functions properly.
        this.$timeout(() => this.initializeResourcesWithProgress());
    }

    public $onChanges(changes: ng.IOnChangesObject): void {
        if (changes.ngModel && !changes.ngModel.isFirstChange()) {
            this.initializeResources();
        }
    }

    public initializeResourcesWithProgress(): ng.IPromise<any> {
        return this.monitoredOperation(this.progressMonitoredOperationName, () => this.initializeResources());
    }

    public addResource(resourceName: string): void {
        this.pcdError.resetErrorForId(this.errorId);

        if (!resourceName || !resourceName.trim()) {
            return;
        }

        if (this.resourceSelector.isResourceNameValid(resourceName)) {
            let displayName = this.resourceSelector.getResourceDisplayName(resourceName);

            if (this.resourceSelector.canAddToExistingResources(this.addedResources, displayName)) {

                this.addedResources.push({
                    id: this.resourceSelector.getResourceId(displayName),
                    displayName: displayName
                });
                this.resourceSelector.addResource(displayName);
            }

            this.textBoxContent = "";
            this.ngModel = this.resourceSelector.getNgModel();
            this.ngModelCtrl.$setViewValue(this.ngModel);

        } else {
            this.pcdError.setErrorForId(this.errorId, useCmsHere_GenericValidationError);
        }
    }

    public onRemoveResource(resourceId: string): void {
        this.removeSelectedResource(resourceId);
    }

    public removeSelectedResource(resourceId: string): void {
        this.addedResources = _.reject(this.addedResources, (resource) => {
            return resource.id === resourceId;
        });

        this.resourceSelector.removeResource(resourceId);

        this.ngModel = this.resourceSelector.getNgModel();
        this.ngModelCtrl.$setViewValue(this.ngModel);
    }

    public shouldAddResourcePadding(): boolean {
        return this.addedResourcePadding && !this.addedResources.length && this.hasFocus;
    }

    public onFocus(): void {
        this.hasFocus = true;
    }

    public onBlur(): void {
        this.hasFocus = false;
        this.addOnBlur && !this.isAutoSuggestEnabled && this.addResource(this.textBoxContent);
    }

    public onKeyup($event: JQueryKeyEventObject): void {
        $event.keyCode === 13 && this.addOnEnter && this.addResource(this.textBoxContent);
    }

    public getSuggestions(lookupText: string): ng.IPromise<Object[]> {
        this.pcdError.resetErrorForId(this.errorId);

        return this.resourceSelector.getResourcesWithPrefix(lookupText);
    }

    private initializeResources(): ng.IPromise<any> {
        return this.resourceSelector.initializeResources()
            .then((resources: SelectorTypes.Resource[]) => {
                this.addedResources = resources.map((resource) => {
                    return {
                        id: resource.id,
                        displayName: resource.displayName,
                        isInvalid: resource.isInvalid
                    };
                });

                return this.addedResources;
            });
    }
}
