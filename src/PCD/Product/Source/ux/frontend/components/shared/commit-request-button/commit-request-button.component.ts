import { Component, Inject } from "../../../module/app.module";
import template = require("./commit-request-button.html!text");

@Component({
    name: "pcdCommitRequestButton",
    options: {
        bindings: {
            errorId: "@pcdErrorId",
            operationName: "@pcdOperationName",
            buttonId: "@?pcdButtonId",
            buttonLabel: "@?pcdButtonLabel",
            buttonOnClickOperation: "&?pcdButtonOnClickOperation",
            buttonDisabled: "=?pcdButtonDisabled"
        },
        template,
        transclude: {
            requestButton: "?pcdCustomRequestButton"
        }
    }
})
@Inject("$transclude")
export default class CommitRequestButtonComponent implements ng.IComponentController {
    //  Input: Error ID if the operation fails.
    public errorId: string;

    //  Input: Operation progress name used for the commit action.
    public operationName: string;

    //  Input: ID to use for the button.
    public buttonId: string;

    //  Input: Text to use for the button.
    public buttonLabel: string;

    //  Input: Execution of the commit action.
    public buttonOnClickOperation: () => void;

    //  Input: If the button should be disabled.
    public buttonDisabled: boolean;

    public isUsingCustomRequestButton: boolean;

    constructor(
        private readonly $transclude: ng.ITranscludeFunction
    ) { }

    public $onInit(): void {
        this.isUsingCustomRequestButton = this.$transclude.isSlotFilled("requestButton");
    }
}
