import { TestSpec, ComponentInstance, SpyCache } from "../../../shared-tests/spec.base";
import PrivacySubjectDemographicIdentifier from "./privacy-subject-demographic-identifier.component";
import PrivacySubjectSelector from "./privacy-subject-selector.component";

describe("Privacy subject demographic identifier component", () => {
    let spec: TestSpec;

    beforeEach(() => {
        spec = new TestSpec();
    });

    //  See Bug 18959069: PCD manual requests for alt-subject combine all ID permutations at the time requests are submitted
    it("creates unique instances of named resource selector model", () => {
        // arrange
        let parent = new PrivacySubjectSelector(spec.$meeComponentRegistry, spec.dataServiceMocks.groundControlApiService.instance);
        let parentSpy = new SpyCache<PrivacySubjectSelector>(parent);
        parentSpy.getFor("privacySubjectChanged").and.stub();

        let meeComponentRegistrySpy = new SpyCache<MeePortal.OneUI.Angular.IMeeComponentRegistryService>
            (spec.$meeComponentRegistry);
        meeComponentRegistrySpy.getFor("getInstanceById").and.returnValue(parent);

        // act
        let component = createComponent();
        const models = [component.instance.citiesSelectorData, component.instance.emailsSelectorData,
            component.instance.identifierFormData, component.instance.namesSelectorData,
            component.instance.phoneNumbersSelectorData, component.instance.postalCodesSelectorData,
            component.instance.regionsSelectorData, component.instance.streetNamesSelectorData,
            component.instance.streetNumbersSelectorData, component.instance.unitNumbersSelectorData];

        // assert
        models.forEach(model => expect(models.filter(m => m === model).length).toBe(1,
            "There should not be any 2 references to model objects, that point to the same object."));
    });

    function createComponent(): ComponentInstance<PrivacySubjectDemographicIdentifier> {
        return spec.createComponent<PrivacySubjectDemographicIdentifier>({
            markup: `<pcd-privacy-subject-demographic-identifier></pcd-privacy-subject-demographic-identifier>`
        });
    }
});

