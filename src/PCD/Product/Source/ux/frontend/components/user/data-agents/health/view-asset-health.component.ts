import { Component, Inject } from "../../../../module/app.module";
import template = require("./view-asset-health.html!text");

import {  HealthIcon, AssetRegistrationStatus }
    from "../../../../shared/registration-status/registration-status-types";
import { StringUtilities } from "../../../../shared/string-utilities";
import { IStringFormatFilter } from "../../../../shared/filters/string-format.filter";
import { HealthFilterType } from "./view-agents-health.component";

const useCmsHere_NoDataTypes = "None";
const useCmsHere_NoSubjectTypes = "None";
const useCmsHere_AssetLabelText = "Asset {0}";

@Component({
    name: "pcdViewAssetHealth",
    options: {
        template,
        bindings: {
            asset: "<",
            indexValue: "@",
            displayByStatus: "@",
        }
    }
})
@Inject("stringFormatFilter")
export default class ViewAssetHealthComponent implements ng.IComponentController {
    // Inputs 
    public asset: AssetRegistrationStatus;
    public indexValue: string;
    public displayByStatus: HealthFilterType;

    public constructor(private stringFormat: IStringFormatFilter) {
    }

    public getStatus(): HealthIcon {
        return this.asset.isComplete ? HealthIcon.healthy : HealthIcon.unhealthy;
    }

    public getDataTypeTags(uris: string[]): string {
        return StringUtilities.getCommaSeparatedList(_.map(this.asset.dataTypeTags, dt => dt.name), {}, useCmsHere_NoDataTypes);
    }

    public getSubjectTypeTags(uris: string[]): string {
        return StringUtilities.getCommaSeparatedList(_.map(this.asset.subjectTypeTags, st => st.name), {}, useCmsHere_NoSubjectTypes);
    }

    public getAssetLabel(): string {
        return this.stringFormat(useCmsHere_AssetLabelText, this.indexValue);
    }
}
