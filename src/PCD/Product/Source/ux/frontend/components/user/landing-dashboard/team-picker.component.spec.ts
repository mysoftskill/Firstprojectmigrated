import * as angular from "angular";

import { TestSpec, SpyCache, ComponentInstance } from "../../../shared-tests/spec.base";
import * as Pdms from "../../../shared/pdms/pdms-types";

import { TeamPickerComponent, TeamPickerModel } from "./team-picker.component";

describe("TeamPickerComponent", () => {
    let spec: TestSpec;

    let testModel: {
        model: TeamPickerModel;
        onChange: () => void;
    };

    beforeEach(() => {
        spec = new TestSpec();
    });

    it("sorts list of teams in alphabetical order", () => {
        // arrange
        testModel = {
            model: createModel(),
            onChange: () => { throw new Error("Not expected at this time."); }
        };
        testModel.model.owners = testModel.model.owners.reverse();
        let component = createComponent();

        // assert
        // Using angular.copy() to strip out properties that Angular adds on to data structures after binding to component.
        expect(angular.copy(component.instance.model.owners)).toEqual(generateListOfTeams());
    });

    it("identifies teams with duplicate names and makes names unique", () => {
        // arrange
        testModel = {
            model: createModel(),
            onChange: () => { throw new Error("Not expected at this time."); }
        };

        // Create duplicates in records. IDs must be unique or createComponent() will fail.
        const firstTeam = testModel.model.owners[0], lastTeam = testModel.model.owners[testModel.model.owners.length - 1];
        testModel.model.owners.push(angular.copy(firstTeam));
        testModel.model.owners[testModel.model.owners.length - 1].id = "Dupe1";
        testModel.model.owners.push(angular.copy(firstTeam));
        testModel.model.owners[testModel.model.owners.length - 1].id = "Dupe2";
        testModel.model.owners.push(angular.copy(lastTeam));
        testModel.model.owners[testModel.model.owners.length - 1].id = "Dupe3";

        testModel.model.owners = testModel.model.owners.reverse();
        let component = createComponent();

        // assert
        // Note: compare only names, because list post-processing doesn't guarantee the order of IDs is going to be the same.
        let expectedList = generateListOfTeams();
        const expectedFirstTeam = expectedList[0], expectedLastTeam = expectedList[expectedList.length - 1];
        expectedList.push(angular.copy(expectedFirstTeam));
        expectedList[expectedList.length - 1].name = "Team A (2)";
        expectedList.push(angular.copy(expectedFirstTeam));
        expectedList[expectedList.length - 1].name = "Team A (3)";
        expectedList.push(angular.copy(expectedLastTeam));
        expectedList[expectedList.length - 1].name = "Team J (2)";
        expect(_.pluck(component.instance.model.owners, "name")).toEqual((<string[]>_.pluck(expectedList, "name")).sort((a, b) => a.localeCompare(b)));
    });

    it("defaults to a first team name of the sorted list, if invalid selectedOwnerId was provided", () => {
        // arrange
        testModel = {
            model: createModel(),
            onChange: () => { throw new Error("Not expected at this time."); }
        };
        testModel.model.owners = testModel.model.owners.reverse();
        testModel.model.selectedOwnerId = "7E8FDC3A-7969-482C-9CD0-265D6E2143C8";
        let component = createComponent();

        // assert
        expect(component.instance.model.selectedOwnerId).toEqual(component.instance.model.owners[0].id);
        expect(component.instance.selectedOwnerName).toEqual(component.instance.model.owners[0].name);
    });

    it("reacts to model changes", () => {
        // arrange
        testModel = {
            model: createModel(),
            onChange: () => { throw new Error("Not expected at this time."); }
        };
        let component = createComponent();

        // assert
        expect(component.instance.model).toEqual(testModel.model);
        expect(component.instance.onChange).toEqual(jasmine.any(Function));

        // re-arrange and trigger change.
        const oldModel = testModel.model;
        component.instance.model = testModel.model = createModel();
        component.instance.$onChanges({
            model: {
                previousValue: oldModel,
                currentValue: testModel.model,
                isFirstChange: () => false
            }
        });

        // assert
        expect(component.instance.model).toEqual(testModel.model);
        expect(component.instance.onChange).toEqual(jasmine.any(Function));
    });

    it("triggers change notification, if user picks valid team in combo box", () => {
        // arrange
        testModel = {
            model: createModel(),
            onChange: jasmine.createSpy("teamPickerOnChange")
        };
        let component = createComponent();

        // act
        component.instance.selectedOwnerName = " team c ";  //  Intentionally lower-cased and has leading/trailing whitespace.
        component.instance.teamListSelectionChanged();

        // assert
        expect(component.instance.model.selectedOwnerId).toBe("Team2");
        expect(testModel.onChange).toHaveBeenCalled();
    });

    it("does not trigger change notification, if user enters invalid team name in combo box", () => {
        // arrange
        testModel = {
            model: createModel(),
            onChange: jasmine.createSpy("teamPickerOnChange")
        };
        let component = createComponent();
        let currentlySelectedOwnerId = component.instance.model.selectedOwnerId;

        // act
        component.instance.selectedOwnerName = "no such team";
        component.instance.teamListSelectionChanged();

        // assert
        expect(component.instance.model.selectedOwnerId).toEqual(currentlySelectedOwnerId);
        expect(testModel.onChange).not.toHaveBeenCalled();
    });

    function createComponent(): ComponentInstance<TeamPickerComponent> {
        return spec.createComponent<TeamPickerComponent>({
            markup: `<pcd-team-picker pcd-team-picker-model="model"
                                      on-change="onChange()"></pcd-team-picker>`,
            data: testModel
        });
    }

    function createModel(): TeamPickerModel {
        return {
            selectedOwnerId: "",
            owners: generateListOfTeams()
        };
    }

    function generateListOfTeams(): Pdms.DataOwner[] {
        return _.range(10).map(idx => {
            return <Pdms.DataOwner> {
                id: `Team${idx}`,
                name: `Team ${String.fromCharCode((idx % 26) + 65)}`,
                description: "",
                alertContacts: [],
                announcementContacts: [],
                sharingRequestContacts: [],
                writeSecurityGroups: []
            };
        });
    }
});
