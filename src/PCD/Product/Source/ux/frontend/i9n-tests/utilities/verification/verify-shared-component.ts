import { browser, element, by, ElementFinder, promise } from "protractor";
import { Search } from "../search";
import { DoneUtil } from "../done-util";
import { generateFuzzyGuidFrom } from "../../../shared/guid";
import { Verify } from "./verify";

export interface EntityNumbering {
    teamNumber: number;
    assetNumber?: number;
    agentNumber?: number;
}
export interface VerifyAgentListSectionArgs {
    entityNumbering: EntityNumbering;
    agentsHaveProdConnection: boolean;
}

export class VerifySharedComponent {
    public static verifyAssetListSectionFor(entityNumbering: EntityNumbering, doneUtil: DoneUtil): void {
        Verify.elementPresentWithId(generateFuzzyGuidFrom(entityNumbering.assetNumber));
        let assetRowEl = Search.elementWithId(generateFuzzyGuidFrom(entityNumbering.assetNumber));

        //  Verify team asset name.
        Verify.elementPresentWithTag("mee-checkbox");
        Verify.elementContainsText(Search.childElementWithTag(assetRowEl, "pcd-asset-group-qualifier-view"),
            `I9n_Asset${entityNumbering.assetNumber}_Team${entityNumbering.teamNumber}`, doneUtil.addPromiseToDone());

        //  Verify info link.
        //  TODO: Make selector more specific than just "a" tag.
        let infoLink = Search.childElementWithTag(Search.childElementWithSelector(assetRowEl, "td[i9n-privacy-actions]"), "a");
        Verify.linkPointsToLocation(
            infoLink,
            `data-assets/manage/${generateFuzzyGuidFrom(entityNumbering.teamNumber)}/privacy-actions/${generateFuzzyGuidFrom(entityNumbering.assetNumber)}`,
            doneUtil.addPromiseToDone()
        );

        //  Verify edit link.
        let editLink = Search.childElementWithSelector(
            Search.childElementWithClass(assetRowEl, "action-list"), "[i9n-edit-asset]");
        Verify.linkPointsToLocation(
            editLink,
            `data-assets/manage/${generateFuzzyGuidFrom(entityNumbering.teamNumber)}/edit/${generateFuzzyGuidFrom(entityNumbering.assetNumber)}`,
            doneUtil.addPromiseToDone()
        );

        //  Verify variants link.
        let variantsLink = Search.childElementWithSelector(
            Search.childElementWithClass(assetRowEl, "action-list"), "[i9n-variants]");
        Verify.linkPointsToLocation(
            variantsLink,
            `data-assets/manage/${generateFuzzyGuidFrom(entityNumbering.teamNumber)}/variants/${generateFuzzyGuidFrom(entityNumbering.assetNumber)}`,
            doneUtil.addPromiseToDone()
        );
    }

    public static verifyAgentListSectionFor(args: VerifyAgentListSectionArgs, doneUtil: DoneUtil): void {
        Verify.elementPresentWithId(generateFuzzyGuidFrom(args.entityNumbering.agentNumber));
        let agentRowEl = Search.elementWithId(generateFuzzyGuidFrom(args.entityNumbering.agentNumber));

        //  Verify team agent name.
        Verify.elementContainsText(agentRowEl, `I9n_Agent${args.entityNumbering.agentNumber}_Team${args.entityNumbering.teamNumber}`,
            doneUtil.addPromiseToDone());

        //  Verify edit link.
        VerifySharedComponent.verifyAgentListActionLink(
            agentRowEl,
            args.entityNumbering,
            "i9n-edit-agent",
            "edit",
            doneUtil
        );

        //  Verify linked data assets link.
        VerifySharedComponent.verifyAgentListActionLink(
            agentRowEl,
            args.entityNumbering,
            "i9n-linked-data-assets",
            "data-assets",
            doneUtil
        );

        //  Verify check health link.
        VerifySharedComponent.verifyAgentListActionLink(
            agentRowEl,
            args.entityNumbering,
            "i9n-check-health",
            "health",
            doneUtil
        );

        let i9nLinkSelector = "i9n-remove-agent";
        Verify.childElementPresentWithSelector(Search.childElementWithClass(agentRowEl, "action-list"), `a[${i9nLinkSelector}]`);
        //  This link eventually opens up the email template, so not verifying anything.
    }

    public static verifyOwnerContextDrawerForTeamNumber(teamNumber: number, doneUtil: DoneUtil): void {
        Verify.elementPresentWithTag("pcd-owner-view-drawer-trigger");

        Verify.elementContainsText(Search.elementWithTag("pcd-drawer-trigger"),
            `I9n_Team${teamNumber}_Name`, doneUtil.addPromiseToDone());

        Verify.elementPresentWithSelector("div[i9n-owner-id]");
        Verify.elementPresentWithSelector("div[i9n-owner-connector-id]");
        Verify.elementPresentWithSelector("div[i9n-owner-description]");
        Verify.elementPresentWithSelector("div[i9n-owner-service-tree-id]");
        Verify.elementPresentWithSelector("pcd-directory-resource-view[i9n-owner-admins]");
        Verify.elementPresentWithSelector("pcd-directory-resource-view[i9n-owner-sharing-request-contacts]");
        Verify.elementPresentWithSelector("pcd-directory-resource-view[i9n-owner-tagged-security-groups]");
        Verify.elementPresentWithSelector("pcd-directory-resource-view[i9n-owner-write-security-groups]");
    }

    public static verifyManualDeleteRequestForm(): void {
        Verify.elementPresentWithSelector("[i9n-cap-id-field]");
        Verify.elementPresentWithSelector("[i9n-subject-priority-selector]");
        Verify.elementPresentWithSelector("[i9n-country-list]");
        Verify.elementPresentWithSelector("[i9n-subject-selector]");
        Verify.elementPresentWithSelector("[i9n-delete-button]");
    }

    public static verifyManualExportRequestForm(): void {
        Verify.elementPresentWithSelector("[i9n-cap-id-field]");
        Verify.elementPresentWithSelector("[i9n-subject-priority-selector]");
        Verify.elementPresentWithSelector("[i9n-country-list]");
        Verify.elementPresentWithSelector("[i9n-subject-selector]");
        Verify.elementPresentWithSelector("[i9n-export-button]");
    }

    public static verifyPrcRequestConfirmationInfo(capId: string, requestId: string, doneUtil: DoneUtil): void {
        Verify.elementPresentWithSelector("[i9n-cap-id]");
        Verify.elementContainsText(
            Search.elementWithSelector("[i9n-cap-id]"), capId, doneUtil.addPromiseToDone());

        Verify.elementPresentWithSelector("[i9n-request-ids]");
        Verify.elementContainsText(
            Search.elementWithSelector("[i9n-request-ids]"), requestId, doneUtil.addPromiseToDone());
    }

    public static verifyPrcRequestConfirmationLinks(doneUtil: DoneUtil): void {
        Verify.elementPresentWithSelector("[i9n-check-status-link]");
        Verify.linkPointsToLocation(
            Search.elementWithSelector("[i9n-check-status-link]"),
            "manual-requests/status",
            doneUtil.addPromiseToDone()
        );

        Verify.elementPresentWithSelector("[i9n-submit-another-request-link]");
        Verify.linkPointsToLocation(
            Search.elementWithSelector("[i9n-submit-another-request-link]"),
            "manual-requests",
            doneUtil.addPromiseToDone()
        );
    }

    /**
     *  Verifies each action link within the rows of agent list. 
     **/
    private static verifyAgentListActionLink(
        agentRowEl: ElementFinder,
        entityNumbering: EntityNumbering,
        i9nSelector: string,
        pathParam: string,
        doneUtil: DoneUtil
    ): void {
        Verify.childElementPresentWithSelector(Search.childElementWithClass(agentRowEl, "action-list"), `a[${i9nSelector}]`);

        let linkEl = Search.childElementWithSelector(Search.childElementWithClass(agentRowEl, "action-list"), `a[${i9nSelector}]`);
        Verify.linkPointsToLocation(
            linkEl,
            `data-agents/manage/${generateFuzzyGuidFrom(entityNumbering.teamNumber)}/${pathParam}/${generateFuzzyGuidFrom(entityNumbering.agentNumber)}`,
            doneUtil.addPromiseToDone()
        );
    }
}
