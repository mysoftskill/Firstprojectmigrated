import { ComponentInstance, SpyCache, TestSpec } from "../../../shared-tests/spec.base";
import { ManualRequestMetadata, MsaSelfAuthSubject, OperationResponse, PrivacySubjectPriority } from "../../../shared/manual-requests/manual-request-types";
import { IPcdErrorService } from "../../../shared/pcd-error.service";
import ManualRequestsDelete from "./delete.component";

describe("Delete manual request", () => {
    let spec: TestSpec;

    let pcdErrorServiceSpy: SpyCache<IPcdErrorService>;
    let correlationContextSpy: SpyCache<Bradbury.ICorrelationContextManager>;

    beforeEach(() => {
        spec = new TestSpec();

        inject((
            _pcdErrorService_: IPcdErrorService,
            _correlationContext_: Bradbury.ICorrelationContextManager
        ) => {
            pcdErrorServiceSpy = new SpyCache(_pcdErrorService_);
            correlationContextSpy = new SpyCache(_correlationContext_);
        });

        spec.dataServiceMocks.pdmsDataService.mockAsyncResultOf("getCountriesList", [{ countryName: "United States", isoCode: "US" }]);
        spec.dataServiceMocks.manualRequestsDataService.mockAsyncResultOf("getDeleteDataTypesOnSubjectRequests", {});

        spec.dataServiceMocks.manualRequestsDataService.getFor("delete").and.stub();
    });

    it("clears errors on init", () => {
        // arrange
        pcdErrorServiceSpy.getFor("resetErrorsForCategory").and.callThrough();

        // act
        let component = createComponent();

        // assert
        expect(pcdErrorServiceSpy.getFor("resetErrorsForCategory")).toHaveBeenCalledWith(component.instance.errorCategory);
    });

    it("resets all errors when delete is clicked", () => {
        // arrange
        let component = createComponent();
        let privacySubjectSelectorSpy = new SpyCache(component.instance.privacySubjectSelector.getInstance());

        pcdErrorServiceSpy.getFor("resetErrorsForCategory").and.callThrough();
        privacySubjectSelectorSpy.getFor("resetErrors").and.stub();
        privacySubjectSelectorSpy.getFor("isValidationSuccessful").and.stub();

        // act
        component.instance.deleteClicked();

        // assert
        expect(pcdErrorServiceSpy.getFor("resetErrorsForCategory")).toHaveBeenCalledWith(component.instance.errorCategory);
        expect(privacySubjectSelectorSpy.getFor("resetErrors")).toHaveBeenCalled();
    });

    it("doesn't send the request when CAP ID is not populated", () => {
        // arrange
        let component = createComponent();
        let privacySubjectSelectorSpy = new SpyCache(component.instance.privacySubjectSelector.getInstance());

        privacySubjectSelectorSpy.getFor("resetErrors").and.stub();

        component.instance.capId = null;
        privacySubjectSelectorSpy.getFor("isValidationSuccessful").and.returnValue(true);

        // act
        component.instance.deleteClicked();

        // assert
        expect(spec.dataServiceMocks.manualRequestsDataService.getFor("delete")).not.toHaveBeenCalled();
    });

    it("doesn't send the request when form data is invalid", () => {
        // arrange
        let component = createComponent();
        let privacySubjectSelectorSpy = new SpyCache(component.instance.privacySubjectSelector.getInstance());

        privacySubjectSelectorSpy.getFor("resetErrors").and.stub();
        privacySubjectSelectorSpy.getFor("getIdentifierFormData").and.stub();

        component.instance.capId = "someValue";
        privacySubjectSelectorSpy.getFor("isValidationSuccessful").and.returnValue(false);

        pcdErrorServiceSpy.getFor("setErrorForId").and.stub();

        // act
        component.instance.deleteClicked();

        // assert
        expect(spec.dataServiceMocks.manualRequestsDataService.getFor("delete")).not.toHaveBeenCalled();
    });

    it("sends the request if all input is valid", () => {
        // arrange
        let component = createComponent();
        let privacySubjectSelectorSpy = new SpyCache(component.instance.privacySubjectSelector.getInstance());
        let countrySelectorSpy = new SpyCache(component.instance.countryListSelector.getInstance());

        let identifierFormData: MsaSelfAuthSubject = {
            kind: "MSA",
            proxyTicket: "proxy ticket"
        };
        privacySubjectSelectorSpy.getFor("resetErrors").and.stub();
        privacySubjectSelectorSpy.getFor("getIdentifierFormData").and.returnValue(identifierFormData);

        component.instance.capId = "cap ID value";
        privacySubjectSelectorSpy.getFor("isValidationSuccessful").and.returnValue(true);
        countrySelectorSpy.getFor("getSelectedCountryIsoCode").and.returnValue("US");

        let manualRequestResponse: OperationResponse = {
            ids: ["123", "456", "789"]
        };
        spec.dataServiceMocks.manualRequestsDataService.getFor("delete").and.returnValue(spec.$promises.resolve(manualRequestResponse));

        spec.$state.spies.getFor("go").and.stub();
        correlationContextSpy.getFor("setProperty").and.stub();
        correlationContextSpy.getFor("deleteProperty").and.stub();

        // act
        component.instance.deleteClicked();

        // assert
        let requestMetadata: ManualRequestMetadata = {
            capId: "cap ID value",
            countryOfResidence: "US",
            priority: PrivacySubjectPriority.Regular
        };
        expect(spec.dataServiceMocks.manualRequestsDataService.getFor("delete")).toHaveBeenCalledWith(identifierFormData, requestMetadata);
        expect(correlationContextSpy.getFor("setProperty")).toHaveBeenCalledWith("scenario-id", "ust.privacy.delete");

        spec.runDigestCycle();

        expect(spec.$state.spies.getFor("go")).toHaveBeenCalledWith(".request-completed", {
            capId: "cap ID value",
            requestIds: manualRequestResponse.ids
        }, { location: "replace" });
        expect(correlationContextSpy.getFor("deleteProperty")).toHaveBeenCalledWith("scenario-id");
    });

    function createComponent(): ComponentInstance<ManualRequestsDelete> {
        return spec.createComponent<ManualRequestsDelete>({
            markup: `<pcd-manual-requests-delete pcd-parent-type="delete"></pcd-manual-requests-delete>`,
        });
    }
});
