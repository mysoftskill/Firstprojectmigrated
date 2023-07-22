import { Component, Inject, Config } from "../../../module/app.module";
import template = require("./ngp-warning-prompt.html!text");

export function registerNgpWarningPromptRoute(
    $stateProvider: ng.ui.IStateProvider,
    parentState: string,
) {
    $stateProvider.state(`${parentState}.ngp-warning-prompt`, {
        views: {
            "modalContent@": {
                template: "<pcd-ngp-warning-prompt></pcd-ngp-warning-prompt>"
            }
        }
    });
}

export interface WarningModalData {
    //  An action to be executed, when user confirms the warning prompt.
    onDismiss?: () => void;
}

// TODO: Create a wrapper component to make a generic warning prompt.
@Component({
    name: "pcdNgpWarningPrompt",
    options: {
        template
    }
})
@Inject("$meeModal")
export class NgpWarningPromptComponent implements ng.IComponentController {
    private data: WarningModalData;

    constructor(private readonly $meeModal: MeePortal.OneUI.Angular.IModalStateService) { }

    public $onInit(): void {
        this.data = this.$meeModal.getData<WarningModalData>();
    }

    private close(): void {
        this.$meeModal.hide("^")
            .then(() => {
                if (this.data.onDismiss) {
                    this.data.onDismiss();
                }
            });
    }
}
