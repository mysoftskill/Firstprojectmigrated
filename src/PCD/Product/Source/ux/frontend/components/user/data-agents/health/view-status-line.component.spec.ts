import { TestSpec, ComponentInstance } from "../../../../shared-tests/spec.base";
import ViewStatusLineComponent from "./view-status-line.component";
import { RegistrationState, HealthIcon } from "../../../../shared/registration-status/registration-status-types";

describe("ViewStatusLineComponent", () => {
    let spec: TestSpec;

    beforeEach(() => {
        spec = new TestSpec();
    });

    describe("When registration state is notApplicable ", () => {
        it("sets icon to Healthy when category is subject-type-tags-status", () => {
            // arrange
            let component = createComponent("subject-type-tags-status", RegistrationState.notApplicable);

            // act/assert
            expect(component.instance.getStatusIcon()).toBe(HealthIcon.healthy);
        });

        it("sets icon to Healthy when category is data-type-tags-status", () => {
            // arrange
            let component = createComponent("data-type-tags-status", RegistrationState.notApplicable);

            // act/assert
            expect(component.instance.getStatusIcon()).toBe(HealthIcon.healthy);
        });

        it("sets icon to Unhealthy when category is assets-status", () => {
            // arrange
            let component = createComponent("assets-status", RegistrationState.notApplicable);

            // act/assert
            expect(component.instance.getStatusIcon()).toBe(HealthIcon.unhealthy);
        });

    });

    describe("When registration state is ValidButTruncated ", () => {

        it("sets icon to Incomplete when category is assets-status", () => {
            // arrange
            let component = createComponent("assets-status", RegistrationState.validButTruncated);

            // act/assert
            expect(component.instance.getStatusIcon()).toBe(HealthIcon.incomplete);
        });

        it("sets icon to Incomplete when category is asset-groups-status", () => {
            // arrange
            let component = createComponent("asset-groups-status", RegistrationState.validButTruncated);

            // act/assert
            expect(component.instance.getStatusIcon()).toBe(HealthIcon.incomplete);
        });
    });

    describe("When registration state is Partial ", () => {

        it("sets icon to Incomplete when category is environments", () => {
            // arrange
            let component = createComponent("environments", RegistrationState.partial);

            // act/assert
            expect(component.instance.getStatusIcon()).toBe(HealthIcon.incomplete);
        });

        it("sets icon to unhealthy when category is asset-status", () => {
            // arrange
            let component = createComponent("asset-status", RegistrationState.partial);

            // act/assert
            expect(component.instance.getStatusIcon()).toBe(HealthIcon.unhealthy);
        });
    });

    function createComponent(category: string, state: RegistrationState): ComponentInstance<ViewStatusLineComponent> {
        return spec.createComponent<ViewStatusLineComponent>({
            markup: `<pcd-view-status-line category="{{category}}" state="state"></pcd-view-status-line>`,
            data: { category, state }
        });
    }
});
