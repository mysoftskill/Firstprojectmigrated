import { TestSpec, SpyCache, ComponentInstance } from "../../../shared-tests/spec.base";
import * as Pdms from "../../../shared/pdms/pdms-types";
import { IPcdErrorService } from "../../../shared/pcd-error.service";
import { IAngularFailureResponse } from "../../../shared/ajax.service";

import { ConfirmationModalActionsComponent, ConfirmationModalData } from "./confirmation-modal-actions.component";

describe("ConfirmationModalActionsComponent", () => {
    let spec: TestSpec;
    let meeModalServiceMock: SpyCache<MeePortal.OneUI.Angular.IModalStateService>;
    let pcdErrorServiceMock: SpyCache<IPcdErrorService>;

    let modalData: ConfirmationModalData;
    let onConfirmSpy: jasmine.Spy;

    beforeEach(() => {
        spec = new TestSpec();

        inject(((_$meeModal_: MeePortal.OneUI.Angular.IModalStateService, _pcdErrorService_: IPcdErrorService) => {
            meeModalServiceMock = new SpyCache(_$meeModal_);
            pcdErrorServiceMock = new SpyCache(_pcdErrorService_);
        }));

        onConfirmSpy = jasmine.createSpy("onConfirm").and.returnValue(spec.$promises.resolve());
        modalData = {
            onConfirm: <() => ng.IPromise<any>>onConfirmSpy,
            returnLocation: "returnLocation"
        };

        meeModalServiceMock.getFor("hide").and.stub();
        meeModalServiceMock.getFor("getData").and.returnValue(modalData);

        pcdErrorServiceMock.getFor("setError").and.stub();
    });

    describe("onCancel", () => {
        it("closes modal with default return location, if returnLocation was not provided", () => {
            // arrange
            delete modalData.returnLocation;
            let component = createComponent();

            // act
            component.instance.onCancel();

            // assert
            expect(meeModalServiceMock.getFor("hide")).toHaveBeenCalledWith({
                stateId: "^"
            });
        });

        it("closes modal with returnLocation (only state name)", () => {
            // arrange
            let component = createComponent();

            // act
            component.instance.onCancel();

            // assert
            expect(meeModalServiceMock.getFor("hide")).toHaveBeenCalledWith({
                stateId: "returnLocation"
            });
        });

        it("closes modal with returnLocationOnCancel (only state name), if provided", () => {
            // arrange
            let component = createComponent();
            modalData.returnLocationOnCancel = "returnLocationOnCancel";

            // act
            component.instance.onCancel();

            // assert
            expect(meeModalServiceMock.getFor("hide")).toHaveBeenCalledWith({
                stateId: "returnLocationOnCancel"
            });
        });

        it("closes modal with returnLocation (state params)", () => {
            // arrange
            let component = createComponent();
            modalData.returnLocation = {
                stateId: "returnLocationStateId",
                stateParams: {
                    test: 123
                },
                stateOptions: {
                    reload: true,
                    location: "replace"
                }
            };

            // act
            component.instance.onCancel();

            // assert
            expect(meeModalServiceMock.getFor("hide")).toHaveBeenCalledWith({
                stateId: "returnLocationStateId",
                stateParams: {
                    test: 123
                },
                stateOptions: {
                    reload: true,
                    location: "replace"
                }
            });
        });

        it("closes modal with returnLocationOnCancel (state params), if provided", () => {
            // arrange
            let component = createComponent();
            modalData.returnLocationOnCancel = {
                stateId: "returnLocationOnCancelStateId",
                stateParams: {
                    test: 123
                },
                stateOptions: {
                    reload: true,
                    location: "replace"
                }
            };

            // act
            component.instance.onCancel();

            // assert
            expect(meeModalServiceMock.getFor("hide")).toHaveBeenCalledWith({
                stateId: "returnLocationOnCancelStateId",
                stateParams: {
                    test: 123
                },
                stateOptions: {
                    reload: true,
                    location: "replace"
                }
            });
        });
    });

    describe("onConfirmClick when onConfirm is successful", () => {
        it("closes modal with default return location, if returnLocation was not provided", () => {
            // arrange
            delete modalData.returnLocation;
            let component = createComponent();

            // act
            component.instance.onConfirmClick();
            spec.runDigestCycle();

            // assert
            expect(meeModalServiceMock.getFor("hide")).toHaveBeenCalledWith({
                stateId: "^"
            });
        });

        it("closes modal with returnLocation (only state name)", () => {
            // arrange
            let component = createComponent();

            // act
            component.instance.onConfirmClick();
            spec.runDigestCycle();

            // assert
            expect(meeModalServiceMock.getFor("hide")).toHaveBeenCalledWith({
                stateId: "returnLocation"
            });
        });

        it("closes modal with returnLocation (state params)", () => {
            // arrange
            let component = createComponent();
            modalData.returnLocation = {
                stateId: "returnLocationStateId",
                stateParams: {
                    test: 123
                },
                stateOptions: {
                    reload: true,
                    location: "replace"
                }
            };

            // act
            component.instance.onConfirmClick();
            spec.runDigestCycle();

            // assert
            expect(meeModalServiceMock.getFor("hide")).toHaveBeenCalledWith({
                stateId: "returnLocationStateId",
                stateParams: {
                    test: 123
                },
                stateOptions: {
                    reload: true,
                    location: "replace"
                }
            });
        });

        it("always closes modal with returnLocation, even if returnLocationOnCancel is provided", () => {
            // arrange
            let component = createComponent();
            modalData.returnLocationOnCancel = "returnLocationOnCancel";

            // act
            component.instance.onConfirmClick();
            spec.runDigestCycle();

            // assert
            expect(meeModalServiceMock.getFor("hide")).toHaveBeenCalledWith({
                stateId: "returnLocation"
            });
        });
    });

    describe("onConfirmClick when onConfirm is unsuccessful", () => {
        it("notifies user about the error", () => {
            // arrange
            let component = createComponent();
            let error: IAngularFailureResponse = {
                errorThrown: "some error",
                jqXHR: <any> {
                    status: 400,
                    statusText: "XHR error"
                },
                textStatus: "text status"
            };
            onConfirmSpy.and.returnValue(spec.$promises.reject(error));

            // act
            component.instance.onConfirmClick();
            spec.runDigestCycle();

            // assert
            expect(pcdErrorServiceMock.getFor("setError")).toHaveBeenCalledWith(error, component.instance.errorCategory);
        });
    });

    describe("canConfirm", () => {
        it("returns true if canConfirm callback was not provided", () => {
            // arrange
            let component = createComponent();

            // act/assert
            expect(component.instance.canConfirm()).toBe(true);
        });

        it("calls canConfirm callback, when provided", () => {
            let canConfirmCalled = false;

            // arrange
            let component = createComponent();
            modalData.canConfirm = () => {
                canConfirmCalled = true;
                return false;
            };

            // act/assert
            expect(component.instance.canConfirm()).toBe(false);    //  Actual value, returned by the callback.
            expect(canConfirmCalled).toBe(true);
        });
    });

    function createComponent(): ComponentInstance<ConfirmationModalActionsComponent> {
        return spec.createComponent<ConfirmationModalActionsComponent>({
            markup: `<pcd-confirmation-modal-actions></pcd-confirmation-modal-actions>`
        });
    }
});
