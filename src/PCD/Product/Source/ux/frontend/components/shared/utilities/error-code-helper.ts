import { IAngularFailureResponse } from "../../../shared/ajax.service";

export class ErrorCodeHelper {

    public static isNetworkError(e: IAngularFailureResponse) {
        return e.jqXHR && e.jqXHR.readyState === 0 /* UNSENT */;
    }

    public static getErrorResponse(e: IAngularFailureResponse) {
        return e.jqXHR && e.jqXHR.responseJSON;
    }

    public static getErrorCode(e: IAngularFailureResponse): string {
        let errorResponse = ErrorCodeHelper.getErrorResponse(e);
        if(errorResponse) {
            if(errorResponse.error === "invalidInput" && (errorResponse.data.target === "hasInitiatedTransferRequests" || errorResponse.data.target === "hasPendingTransferRequests")) {
                return errorResponse.data.target;
            }
        }
        return errorResponse && errorResponse.error;
    }

    public static getErrorTarget(e: IAngularFailureResponse): string {
        let errorResponse = ErrorCodeHelper.getErrorResponse(e);
        return errorResponse && errorResponse.data && errorResponse.data.target;
    }
}
