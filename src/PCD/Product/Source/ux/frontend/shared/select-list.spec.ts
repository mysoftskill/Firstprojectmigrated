import * as angular from "angular";
import * as SelectList from "./select-list";

describe("select-list", () => {
    let testModel: SelectList.Model;

    beforeEach(() => {
        testModel = {
            items: [{
                id: "518BA2D5-0211-4252-8240-DCD67C04F59C",
                label: "some label"
            }, {
                id: "14543464-1C06-4C06-9125-ED16E8372C9B",
                label: "some label"
            }]
        };
    });

    describe("enforceModelConstraints", () => {
        it("ignores falsy selectedId", () => {
            const modelCopy = angular.copy(testModel);

            SelectList.enforceModelConstraints(testModel);
            expect(testModel).toEqual(modelCopy);
        });

        it("keeps selectedId, if item with the ID is present", () => {
            testModel.selectedId = "14543464-1C06-4C06-9125-ED16E8372C9B";

            SelectList.enforceModelConstraints(testModel);
            expect(testModel.selectedId).toBe("14543464-1C06-4C06-9125-ED16E8372C9B");
        });

        it("resets selectedId, if item with the ID is not present", () => {
            testModel.selectedId = "EC89DAB4-CD7A-4C03-8A9A-31D675B8457B";

            SelectList.enforceModelConstraints(testModel);
            expect(testModel.selectedId).toBeFalsy();
        });
    });
});
