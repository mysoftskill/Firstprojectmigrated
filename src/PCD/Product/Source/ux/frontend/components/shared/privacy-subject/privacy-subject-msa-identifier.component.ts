import { Component, Inject } from "../../../module/app.module";
import template = require("./privacy-subject-msa-identifier.html!text");

import * as ManualRequest from "../../../shared/manual-requests/manual-request-types";
import PrivacySubjectSelector from "./privacy-subject-selector.component";
import { IPcdErrorService } from "../../../shared/pcd-error.service";
import { Lazy } from "../../../shared/utilities/lazy";

const defaultErrorCategory = "msa-identifier";

@Component({
    name: "pcdPrivacySubjectMsaIdentifier",
    options: {
        template,
        bindings: {
            errorCategory: "@?pcdErrorCategory"
        }
    }
})
@Inject("pcdErrorService", "$meeComponentRegistry")
export default class PrivacySubjectMsaIdentifier implements ManualRequest.PrivacySubjectIdentifierFormEntryComponent {
    public errorCategory: string;
    public identifierType = ManualRequest.PrivacySubjectDetailTypeId[ManualRequest.PrivacySubjectDetailTypeId.MSA];

    public identifierFormData: ManualRequest.MsaSelfAuthSubject;
    public parentCtrl: Lazy<PrivacySubjectSelector>;

    public constructor(
        private readonly pcdError: IPcdErrorService,
        private readonly $meeComponentRegistry: MeePortal.OneUI.Angular.IMeeComponentRegistryService
    ) {
        this.$meeComponentRegistry.register("PrivacySubject", this.identifierType, this);

        this.parentCtrl = new Lazy<PrivacySubjectSelector>(() =>
            this.$meeComponentRegistry.getInstanceById("PrivacySubjectSelector"));
    }

    public $onInit(): void {
        if (!this.errorCategory) {
            this.errorCategory = defaultErrorCategory;
        }

        this.resetForm();
        
        this.parentCtrl.getInstance().privacySubjectChanged();
    }

    public $onDestroy(): void {
        this.$meeComponentRegistry.deregister(this.identifierType);

        this.parentCtrl.getInstance().privacySubjectChanged();
    }

    public hasDataEntryErrors(): boolean {
        if (!this.identifierFormData.proxyTicket) {
            this.pcdError.setRequiredFieldErrorForId(`${this.errorCategory}.proxy-ticket`);
            return true;
        }
        return false;
    }

    public resetErrors(): void {
        this.pcdError.resetErrorsForCategory(this.errorCategory);
    }

    public resetForm(): void {
        this.resetErrors();
        this.identifierFormData = {
            kind: "MSA",
            proxyTicket: null
        };
    }

    public getIdentifierFormData(): ManualRequest.MsaSelfAuthSubject {
        return this.identifierFormData;
    }
}
