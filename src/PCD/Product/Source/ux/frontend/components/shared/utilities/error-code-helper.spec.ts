import { TestSpec } from "../../../shared-tests/spec.base";
import * as Pdms from "../../../shared/pdms/pdms-types";

import { ErrorCodeHelper } from "./error-code-helper";
import { IAngularFailureResponse, JQueryXHRWithErrorResponse } from "../../../shared/ajax.service";

describe("ErrorCodeHelper", () => {
    let spec: TestSpec;
    let failureResponse: IAngularFailureResponse;

    beforeEach(() => {
        spec = new TestSpec();

        failureResponse = <IAngularFailureResponse> {
            jqXHR: <Partial<JQueryXHRWithErrorResponse>> {
                readyState: 0,
                responseJSON: {
                    error: "",
                    data: {
                        target: ""
                    }
                }
            }
        };
    });

    describe("isNetworkError", () => {
        it("returns true if the readyState is 0 (UNSENT)", () => {
            failureResponse.jqXHR = <JQueryXHRWithErrorResponse>{ readyState: 0 };

            expect(ErrorCodeHelper.isNetworkError(failureResponse)).toBeTruthy();
        });

        it("returns false if the readyState is 4 (DONE)", () => {
            failureResponse.jqXHR = <JQueryXHRWithErrorResponse> { readyState: 4 };

            expect(ErrorCodeHelper.isNetworkError(failureResponse)).toBeFalsy();
        });
    });

    describe("getErrorResponse", () => {
        it("returns the response json if available", () => {
            let responseJson = {
                error: "someError",
                data: {
                    target: "someTarget"
                }
            };
            failureResponse.jqXHR.responseJSON = responseJson;

            expect(ErrorCodeHelper.getErrorResponse(failureResponse)).toEqual(responseJson);
        });

        it("returns null if there is no response json", () => {
            failureResponse.jqXHR.responseJSON = null;

            expect(ErrorCodeHelper.getErrorResponse(failureResponse)).toBeNull();
        });
    });

    describe("getErrorCode", () => {
        it("returns the error if available", () => {
            let responseJson = {
                error: "someError",
                data: {
                    target: "someTarget"
                }
            };
            failureResponse.jqXHR.responseJSON = responseJson;

            expect(ErrorCodeHelper.getErrorCode(failureResponse)).toEqual(responseJson.error);
        });

        it("returns null if there is no error", () => {
            failureResponse.jqXHR.responseJSON.error = null;

            expect(ErrorCodeHelper.getErrorCode(failureResponse)).toBeNull();
        });

        it("checks the \"hasInitiatedTransferRequests\" error case", () => {
            let responseJson = {
                error: "invalidInput",
                data: {
                    target: "hasInitiatedTransferRequests"
                }
            };
            failureResponse.jqXHR.responseJSON = responseJson;

            expect(ErrorCodeHelper.getErrorCode(failureResponse)).toEqual(responseJson.data.target);
        });

        it("checks the \"hasPendingTransferRequests\" error case", () => {
            let responseJson = {
                error: "invalidInput",
                data: {
                    target: "hasPendingTransferRequests"
                }
            };
            failureResponse.jqXHR.responseJSON = responseJson;

            expect(ErrorCodeHelper.getErrorCode(failureResponse)).toEqual(responseJson.data.target);
        });
    });

    describe("getErrorTarget", () => {
        it("returns the error target if available", () => {
            let responseJson = {
                error: "someError",
                data: {
                    target: "someTarget"
                }
            };
            failureResponse.jqXHR.responseJSON = responseJson;

            expect(ErrorCodeHelper.getErrorTarget(failureResponse)).toEqual(responseJson.data.target);
        });

        it("returns null if there is no error target", () => {
            failureResponse.jqXHR.responseJSON.data.target = null;

            expect(ErrorCodeHelper.getErrorTarget(failureResponse)).toBeNull();
        });
    });
});
