import { Component, Inject } from "../../../module/app.module";
import template = require("./lockdown-warning.html!text");

import { ILockdownService } from "../../../shared/lockdown.service";
import { IContactService } from "../../../shared/contact.service";

const useCmsHere_ContactAdminsLinkText = "Contact Us";

@Component({
    name: "pcdLockdownWarning",
    options: {
        template,
        bindings: {
            relatedEntityId: "<?pcdRelatedEntityId",
        }
    }
})
@Inject("lockdownService", "contactService")
export default class LockdownWarningComponent implements ng.IComponentController {
    /** 
     * Input 
     **/
    public relatedEntityId = "";

    public contactAdminsLinkText = useCmsHere_ContactAdminsLinkText;

    constructor(
        private readonly lockdown: ILockdownService,
        private readonly contactService: IContactService) { }

    public showLockdownWarning(): boolean {
        return this.lockdown.isActive();
    }

    public getLockdownMessage(): string {
        return this.lockdown.getMessage();
    }

    public contactAdmins(): void {
        this.contactService.requestAdminAssistance("lockdown", { entityId: this.relatedEntityId });
    }
}
