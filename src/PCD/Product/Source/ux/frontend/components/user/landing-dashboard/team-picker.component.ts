import { Component, Inject } from "../../../module/app.module";
import template = require("./team-picker.html!text");

import * as Pdms from "../../../shared/pdms/pdms-types";

export interface TeamPickerModel {
    selectedOwnerId: string;
    owners: Pdms.DataOwner[];
}

/** 
 * Start value for the dupe index counter. 
 **/
const DupeStartIndex = 2;

@Component({
    name: "pcdTeamPicker",
    options: {
        template,
        bindings: {
            model: "<pcdTeamPickerModel",
            onChange: "&"
        }
    }
})
export class TeamPickerComponent implements ng.IComponentController {
    /** 
     * Input: team picker model instance. 
     **/
    public model: TeamPickerModel;
    /** 
     * Input: callback that occurs after team was selected by the user. 
     **/
    public onChange: () => void;

    public selectedOwnerName: string;

    public $onChanges(changes: ng.IOnChangesObject): void {
        if (changes["model"]) {
            this.modelChanged();
        }
    }

    public teamListSelectionChanged(): void {
        if (this.selectedOwnerName) {
            const searchableSelectedOwnerName = this.selectedOwnerName.trim().toLocaleUpperCase();

            //  Combo doesn't support item IDs, so search by name, it's unique, but there's a possibility it's not found.
            let selectedOwner = this.model.owners.filter(owner => owner.name.toLocaleUpperCase() === searchableSelectedOwnerName)[0];
            if (selectedOwner) {
                this.model.selectedOwnerId = selectedOwner.id;
                this.onChange();
            }
        }
    }

    private modelChanged(): void {
        this.model.owners = this.organizeListOfOwners(this.model.owners);

        //  Component model operates on owner ID, but the implementation cannot use it directly, must use owner name instead.
        let selectedOwner =
            (this.model.selectedOwnerId && this.model.owners.filter(owner => owner.id === this.model.selectedOwnerId)[0])
            || this.model.owners[0];

        this.selectedOwnerName = selectedOwner && selectedOwner.name;
        this.model.selectedOwnerId = selectedOwner && selectedOwner.id;
    }

    private organizeListOfOwners(owners: Pdms.DataOwner[]): Pdms.DataOwner[] {
        let sortedOwners = owners.sort((a, b) => a.name.localeCompare(b.name));

        //  In some cases owner names can be duplicated and this makes it impossible to select correct 
        //  team (see 15985267). Ensure that all names displayed by combo are unique.
        let previousOwnerName = "";
        let dupeIdx = DupeStartIndex;
        for (let i = 0; i < sortedOwners.length; i++) {
            const ownerName = sortedOwners[i].name.toLocaleUpperCase();

            if (previousOwnerName === ownerName) {
                sortedOwners[i].name = `${sortedOwners[i].name} (${dupeIdx})`;
                dupeIdx++;
            } else {
                dupeIdx = DupeStartIndex;
            }

            previousOwnerName = ownerName;
        }

        return sortedOwners;
    }
}
