import { Inject, Service } from "../module/app.module";
import { IAngularFailureResponse } from "./ajax.service";
import { ErrorCodeHelper } from "../components/shared/utilities/error-code-helper";

//  General error strings.
const useCmsHere_FieldRequired = "This field is required.";
const useCmsHere_GenericError = "It appears that something went wrong. If you believe this is an error, please file a support request at https://aka.ms/ngpdataagentsupport and include a screenshot of the error.";
const useCmsHere_GenericValidationError = "It appears that there's something wrong with your input.";

//  Target error strings.
const useCmsHere_BadSecurityGroups = "It appears that you do not belong to any of the security groups allowed to make changes in PCD. Please make sure that you are a member of at least one of the team's security groups on the 'Write security groups' list.";
const useCmsHere_BadServiceTree = "It appears that you are not allowed to modify Service Tree information of the team that you have selected. Please contact administrators of the service tree team to give you access and allow up to 24 hours for Service Tree changes to update in our system.";
const useCmsHere_HasDependentEntityConflict = "It appears that you are not allowed to delete this data since it has some dependent entities. Please remove all dependent entities to delete this data.";
const useCmsHere_HasPendingCommandsConflict = "It appears that this data agent has some pending commands. Please process all pending commands to delete this data agent.";
const useCmsHere_ImmutableValue = "It appears that you're trying to modify an immutable value. Please note this cannot be changed.";
const useCmsHere_NetworkIssue = "Please check your internet connection and retry the operation.";
const useCmsHere_InvalidCharacter = "It appears that you're using an invalid character. Please make sure you're using only alpha-numerical characters for this field.";
const useCmsHere_NotAuthorized = "It appears that you are not allowed to perform the operation you have requested.";
const useCmsHere_NotAuthorizedServiceViewer = "It appears that you are not allowed to perform the operation you have requested. Please visit https://osgwiki.com/wiki/Asimov/Access for information on how to request access.";
const useCmsHere_IcmConnectorDoesNotExist = "We could not create a test incident via IcM connector with an ID that you have entered. Please make sure you're using 'NGP Common Infra' alert source when configuring your connector in IcM.";
const useCmsHere_ETagMismatch = "There was a problem saving your changes, please try again later.";

//  Dictionary of error codes to the generic string to use as an error message.
export const DefaultErrorMessages: IPcdErrorMessages = {
    badSecurityGroups: useCmsHere_BadSecurityGroups,
    badServiceTree: useCmsHere_BadServiceTree,
    hasDependentEntity: useCmsHere_HasDependentEntityConflict,
    hasPendingCommands: useCmsHere_HasPendingCommandsConflict,
    immutableValue: useCmsHere_ImmutableValue,
    invalidCharacter: useCmsHere_InvalidCharacter,
    invalidInput: useCmsHere_GenericValidationError,
    notAuthorized: useCmsHere_NotAuthorized,
    notAuthorized_serviceViewer: useCmsHere_NotAuthorizedServiceViewer,
    doesNotExist_icm: useCmsHere_IcmConnectorDoesNotExist,
    eTagMismatch: useCmsHere_ETagMismatch
};

//  Dictionary of error codes to the generic string to use as a target error message.
export const DefaultTargetErrorMessages: IPcdErrorMessages = {
    invalidCharacter: useCmsHere_InvalidCharacter
};

//  Non-localized error code used to describe issues in sending an ajax request.
export const NetworkErrorCodeId = "networkError";
//  Non-localized error ID used to identify the html element to set the generic error on.
export const GenericErrorId = "generic";

//  Named map types.
export interface IPcdErrorMessages {
    [errorCode: string]: string;
}
export interface IPcdErrorTargets<T> {
    [target: string]: T;
}

//  Optional maps to provide more specific error detials, if applicable.
export interface PcdErrorLookups {
    //  Dictionary of error codes to generic error messages to display on the client.
    errorMessages?: IPcdErrorMessages;

    //  A generic string to be used to be used for all unspecified errors.
    genericErrorMessage?: string;

    //  Boolean used in conjunction with `genericErrorMessage` to indicate if the cV should be shown along side the message.
    showCvOnGenericErrorMessage?: boolean;

    //  Dictionary of targets to client specific error IDs (excluding error category).
    targetErrorIds?: IPcdErrorTargets<string>;

    //  Dictionary of targets to a nested dictionary of error codes to target error messages to display on the client.
    targetErrorMessages?: IPcdErrorTargets<IPcdErrorMessages>;
}

//  The configurations that can override generic errors.
export interface PcdErrorOverrides {
    //  Mappings of client specific error details.
    overrides?: PcdErrorLookups;

    //  Error ID used to set generic errors.
    genericErrorId?: string;
}

//  Details that can be extracted from the failure response.
export interface PcdErrorData {
    //  String to show for the general failure case
    clientErrorMessage: string;

    //  Error ID used to set target specific errors, if applicable.
    clientTargetErrorId?: string;

    //  String to show for the specific input faliure case, which is only applicable when clientTargetErrorId exists.
    clientTargetErrorMessage?: string;
}

//  Provides facilities to set and reset errors on the client.
export interface IPcdErrorService {
    //  Sets the most specific error available on the client.
    setError(e: IAngularFailureResponse, errorCategory: string, overrides?: PcdErrorOverrides): void;

    //  Sets the error corresponding to error ID.
    setErrorForId(id: string, error: string | boolean): void;

    //  Sets a required field error on the provided error ID.
    setRequiredFieldErrorForId(id: string): void;

    //  Extracts the most specific error details.
    getErrorData(e: IAngularFailureResponse, lookups: PcdErrorLookups): PcdErrorData;

    //  Resets all errors that belong to the specified error category.
    resetErrorsForCategory(errorCategory: string): void;

    //  Resets the error for specific ID (or multiple specific IDs).
    resetErrorForId(...errorIds: string[]): void;

    //  Checks if there are any errors in the specific category.
    hasErrorsInCategory(errorCategory: string): boolean;
}

@Inject("$meeErrors", "$window")
@Service({
    name: "pcdErrorService"
})
class PcdErrorService implements IPcdErrorService {
    constructor(
        private readonly $meeErrors: MeePortal.OneUI.Angular.IMeeErrorsService,
        private readonly $window: ng.IWindowService) {
    }

    public setError(e: IAngularFailureResponse, errorCategory: string, overrides?: PcdErrorOverrides) {
        let errorData: PcdErrorData = this.getErrorData(e, overrides && overrides.overrides);

        let clientErrorId = (overrides && overrides.genericErrorId) || GenericErrorId;
        this.$meeErrors.setError(`${errorCategory}.${clientErrorId}`, errorData.clientErrorMessage);

        if (errorData.clientTargetErrorId) {
            this.$meeErrors.setError(`${errorCategory}.${errorData.clientTargetErrorId}`,
                errorData.clientTargetErrorMessage || true);
        }
    }

    public getErrorData(e: IAngularFailureResponse, lookups: PcdErrorLookups | null): PcdErrorData {
        if (ErrorCodeHelper.isNetworkError(e)) {
            return {
                clientErrorMessage: `${NetworkErrorCodeId} - ${useCmsHere_NetworkIssue}`
            };
        }

        let errorCode = ErrorCodeHelper.getErrorCode(e) || "";

        let clientErrorMessage = "";
        // Try to get a specific error message.
        if (lookups) {
            if (lookups.errorMessages && lookups.errorMessages[errorCode]) {
                // Specific message for an error code.
                clientErrorMessage = lookups.errorMessages[errorCode];
            } else if (lookups.genericErrorMessage) {
                // Specific message for any unhandled error code.
                if (lookups.showCvOnGenericErrorMessage) {
                    clientErrorMessage = `${lookups.genericErrorMessage} cV: ${this.$window.BradburyTelemetry.cv.getCurrentCvValue()}`;
                } else {
                    clientErrorMessage = lookups.genericErrorMessage;
                }
            }
        }
        // Try to default to common and known error codes.
        if (!clientErrorMessage && DefaultErrorMessages[errorCode]) {
            clientErrorMessage = DefaultErrorMessages[errorCode];
        }
        // For the uncommon or unknown error, dump the available information into the message.
        if (!clientErrorMessage) {
            clientErrorMessage = `${errorCode} - ${useCmsHere_GenericError} cV:${this.$window.BradburyTelemetry.cv.getCurrentCvValue()}`;
        }

        let errorTarget = ErrorCodeHelper.getErrorTarget(e);
        let clientTargetErrorId: string;
        let clientTargetErrorMessage: string;
        if (errorTarget) {
            clientTargetErrorId =
                (lookups && lookups.targetErrorIds && lookups.targetErrorIds[errorTarget]) ||
                errorTarget;

            clientTargetErrorMessage =
                (lookups && lookups.targetErrorMessages && lookups.targetErrorMessages[errorTarget] && lookups.targetErrorMessages[errorTarget][errorCode]) ||
                DefaultTargetErrorMessages[errorCode];
        }

        return { clientErrorMessage, clientTargetErrorId, clientTargetErrorMessage };
    }

    public setErrorForId(id: string, error: string | boolean): void {
        this.$meeErrors.setError(id, error);
    }

    public setRequiredFieldErrorForId(id: string): void {
        this.$meeErrors.setError(id, useCmsHere_FieldRequired);
    }

    public hasErrorsInCategory(errorCategory: string): boolean {
        return this.$meeErrors.hasErrorsInCategory(errorCategory);
    }

    public resetErrorsForCategory(errorCategory: string): void {
        this.$meeErrors.resetCategory(errorCategory);
    }

    public resetErrorForId(...errorIds: string[]): void {
        if (!errorIds.length) {
            throw new Error("resetErrorForId: At least one error ID must be specified.");
        }

        errorIds.forEach(id => this.$meeErrors.reset(id));
    }
}
