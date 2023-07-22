import { TestSpec, SpyCache } from "../shared-tests/spec.base";

import { IAngularFailureResponse, JQueryXHRWithErrorResponse } from "./ajax.service";
import { DefaultErrorMessages, PcdErrorOverrides, DefaultTargetErrorMessages, GenericErrorId, IPcdErrorService, PcdErrorLookups, PcdErrorData, NetworkErrorCodeId } from "./pcd-error.service";

describe("PCD error service", () => {
    let spec: TestSpec;
    let pcdErrorService: IPcdErrorService;
    let meeErrorServiceMock: SpyCache<MeePortal.OneUI.Angular.IMeeErrorsService>;

    beforeEach(() => {
        spec = new TestSpec();

        inject((_pcdErrorService_: IPcdErrorService,
            _$meeErrors_: MeePortal.OneUI.Angular.IMeeErrorsService) => {
            pcdErrorService = _pcdErrorService_;
            meeErrorServiceMock = new SpyCache(_$meeErrors_);
        });
    });

    describe("setError", () => {
        let getErrorSpy: SpyCache<IPcdErrorService>;
        let errorData: PcdErrorData;

        beforeEach(() => {
            getErrorSpy = new SpyCache(pcdErrorService);
            errorData = {
                clientErrorMessage: "Some message.",
                clientTargetErrorId: "",
                clientTargetErrorMessage: ""
            };
        });

        it("uses the override error id", () => {
            meeErrorServiceMock.getFor("setError").and.stub();
            let overrides: PcdErrorOverrides = { genericErrorId: "OverrideId" };
            getErrorSpy.getFor("getErrorData").and.returnValue(errorData);

            pcdErrorService.setError(null, "exCategory", overrides);

            expect(meeErrorServiceMock.getFor("setError")).toHaveBeenCalledWith("exCategory.OverrideId", "Some message.");
        });

        it("uses the default error id", () => {
            meeErrorServiceMock.getFor("setError").and.stub();
            getErrorSpy.getFor("getErrorData").and.returnValue(errorData);

            pcdErrorService.setError(null, "exCategory");

            expect(meeErrorServiceMock.getFor("setError")).toHaveBeenCalledWith(`exCategory.${GenericErrorId}`, "Some message.");
        });

        it("sets error on target when client target id is found", () => {
            meeErrorServiceMock.getFor("setError").and.stub();
            errorData.clientTargetErrorId = "targetId";
            getErrorSpy.getFor("getErrorData").and.returnValue(errorData);

            pcdErrorService.setError(null, "exCategory");

            expect(meeErrorServiceMock.getFor("setError")).toHaveBeenCalledWith(`exCategory.${GenericErrorId}`, "Some message.");
            expect(meeErrorServiceMock.getFor("setError")).toHaveBeenCalledWith("exCategory.targetId", true);
        });

        it("sets error on target with message when client target id and message are found", () => {
            meeErrorServiceMock.getFor("setError").and.stub();
            errorData.clientTargetErrorId = "targetId";
            errorData.clientTargetErrorMessage = "Target message.";
            getErrorSpy.getFor("getErrorData").and.returnValue(errorData);

            pcdErrorService.setError(null, "exCategory");

            expect(meeErrorServiceMock.getFor("setError")).toHaveBeenCalledWith(`exCategory.${GenericErrorId}`, "Some message.");
            expect(meeErrorServiceMock.getFor("setError")).toHaveBeenCalledWith("exCategory.targetId", "Target message.");
        });
    });

    describe("getErrorData", () => {
        let failureResponse: IAngularFailureResponse;
        let testOverrides: PcdErrorLookups;

        beforeEach(() => {
            failureResponse = <IAngularFailureResponse> {
                jqXHR: <Partial<JQueryXHRWithErrorResponse>> {
                    responseJSON: {
                        error: "",
                        data: {
                            target: ""
                        }
                    }
                }
            };

            testOverrides = {
                errorMessages: {
                    hitOverrideErrorCode: "Found error message."
                },
                targetErrorIds: {
                    hitOverrideTargetId: "Found target id."
                },
                targetErrorMessages: {
                    hitOverrideTargetId: {
                        hitOverrideErrorCode: "Found target id for error code."
                    }
                }
            };
        });

        describe("network errors", () => {
            it("returns a standard network error code in the message", () => {
                failureResponse.jqXHR = <JQueryXHRWithErrorResponse> { readyState: 0 };

                let result = pcdErrorService.getErrorData(failureResponse, testOverrides);

                expect(result.clientErrorMessage).toContain(`${NetworkErrorCodeId} - `);
            });
        });

        describe("client error message", () => {
            it("returns the message in error lookups", () => {
                failureResponse.jqXHR.responseJSON.error = "hitOverrideErrorCode";

                let result = pcdErrorService.getErrorData(failureResponse, testOverrides);

                expect(result.clientErrorMessage).toBe("Found error message.");
            });

            it("returns the default message if not found in error lookups", () => {
                let knownErrorCode = Object.keys(DefaultErrorMessages)[0];
                if (!knownErrorCode) {
                    failureResponse.jqXHR.responseJSON.error = knownErrorCode;

                    let result = pcdErrorService.getErrorData(failureResponse, testOverrides);

                    expect(result.clientErrorMessage).toBe(DefaultErrorMessages[knownErrorCode]);
                }
            });

            it("returns a generic message if not found in default lookup", () => {
                failureResponse.jqXHR.responseJSON.error = "missAll";

                let result = pcdErrorService.getErrorData(failureResponse, testOverrides);

                expect(result.clientErrorMessage).toContain("missAll - ");
            });
        });

        describe("generic error message", () => {
            it("returns the specific message over the generic if both are specified", () => {
                failureResponse.jqXHR.responseJSON.error = "hitOverrideErrorCode";
                testOverrides.genericErrorMessage = "Custom catch-all messaging.";

                let result = pcdErrorService.getErrorData(failureResponse, testOverrides);

                expect(result.clientErrorMessage).toBe("Found error message.");
            });

            it("returns the specified generic message in error lookups", () => {
                testOverrides.genericErrorMessage = "Custom catch-all messaging.";
                failureResponse.jqXHR.responseJSON.error = "missAll";

                let result = pcdErrorService.getErrorData(failureResponse, testOverrides);

                expect(result.clientErrorMessage).toBe("Custom catch-all messaging.");
            });

            it("returns the specified generic message in error lookups with cV if specified", () => {
                testOverrides.genericErrorMessage = "Custom catch-all messaging.";
                testOverrides.showCvOnGenericErrorMessage = true;
                failureResponse.jqXHR.responseJSON.error = "missAll";

                let result = pcdErrorService.getErrorData(failureResponse, testOverrides);

                expect(result.clientErrorMessage).toContain("cV:");
            });

            it("returns a default message if not found in default lookup", () => {
                failureResponse.jqXHR.responseJSON.error = "customTestErrorCode";
                DefaultErrorMessages["customTestErrorCode"] = "Default message for customTestErrorCode";

                let result = pcdErrorService.getErrorData(failureResponse, testOverrides);

                expect(result.clientErrorMessage).toBe("Default message for customTestErrorCode");
            });

            it("returns a generic message if not found in default lookup", () => {
                failureResponse.jqXHR.responseJSON.error = "missAll";

                let result = pcdErrorService.getErrorData(failureResponse, testOverrides);

                expect(result.clientErrorMessage).toContain("missAll - ");
            });
        });

        describe("client target id", () => {
            it("returns the client target id in error lookups", () => {
                failureResponse.jqXHR.responseJSON.data.target = "hitOverrideTargetId";

                let result = pcdErrorService.getErrorData(failureResponse, testOverrides);

                expect(result.clientTargetErrorId).toBe("Found target id.");
            });

            it("returns the failure response target if not found in error lookups", () => {
                failureResponse.jqXHR.responseJSON.data.target = "missOverrides";

                let result = pcdErrorService.getErrorData(failureResponse, testOverrides);

                expect(result.clientTargetErrorId).toBe("missOverrides");
            });

            it("returns undefined if no target specified in the failure response", () => {
                failureResponse.jqXHR.responseJSON.data.target = "";

                let result = pcdErrorService.getErrorData(failureResponse, testOverrides);

                expect(result.clientTargetErrorId).not.toBeDefined();
            });
        });

        describe("client target message", () => {
            it("returns the target message in error lookups", () => {
                failureResponse.jqXHR.responseJSON.error = "hitOverrideErrorCode";
                failureResponse.jqXHR.responseJSON.data.target = "hitOverrideTargetId";

                let result = pcdErrorService.getErrorData(failureResponse, testOverrides);

                expect(result.clientTargetErrorMessage).toBe("Found target id for error code.");
            });

            it("returns the default target message if not found in error lookups", () => {
                let knownErrorCode = Object.keys(DefaultTargetErrorMessages)[0];
                if (!knownErrorCode) {
                    failureResponse.jqXHR.responseJSON.error = knownErrorCode;

                    let result = pcdErrorService.getErrorData(failureResponse, testOverrides);

                    expect(result.clientTargetErrorMessage).toBe(DefaultTargetErrorMessages[knownErrorCode]);
                }
            });

            it("returns undefined if no target specified in the failure response", () => {
                failureResponse.jqXHR.responseJSON.data.target = "";

                let result = pcdErrorService.getErrorData(failureResponse, testOverrides);

                expect(result.clientTargetErrorMessage).not.toBeDefined();
            });
        });
    });

    describe("reset errors", () => {
        it("resets provided category", () => {
            meeErrorServiceMock.getFor("resetCategory").and.stub();

            pcdErrorService.resetErrorsForCategory("exCategory");

            expect(meeErrorServiceMock.getFor("resetCategory")).toHaveBeenCalledWith("exCategory");
        });

        it("resets provided error ID", () => {
            meeErrorServiceMock.getFor("reset").and.stub();

            pcdErrorService.resetErrorForId("exID");

            expect(meeErrorServiceMock.getFor("reset")).toHaveBeenCalledWith("exID");
        });

        it("resets provided error IDs", () => {
            meeErrorServiceMock.getFor("reset").and.stub();

            pcdErrorService.resetErrorForId("exID1", "exID2", "exID3");

            expect(meeErrorServiceMock.getFor("reset")).toHaveBeenCalledWith("exID1");
            expect(meeErrorServiceMock.getFor("reset")).toHaveBeenCalledWith("exID2");
            expect(meeErrorServiceMock.getFor("reset")).toHaveBeenCalledWith("exID3");
        });

        it("throws, if no error ID provided", () => {
            expect(() => pcdErrorService.resetErrorForId()).toThrowError();
        });
    });
});
