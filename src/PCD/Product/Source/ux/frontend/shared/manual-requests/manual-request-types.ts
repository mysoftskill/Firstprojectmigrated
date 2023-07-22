import * as Shared from "./../shared-types";
import { PcdErrorOverrides } from "../pcd-error.service";

const useCmsHere_IdentifierDataInsufficient = "You have not provided sufficient data to send the request. Please provide more information.";
const useCmsHere_InvalidProxyTicket = "It appears that you're using an invalid proxy ticket.";
const useCmsHere_ExpiredProxyTicket = "It appears that you're using an expired proxy ticket. Please file an issue using 'Issues and feedback', as the issue will require manual intervention to resolve.";
const useCmsHere_RequiredForAddress = "This field is required when specifying an address.";

export const Manual_Request_Error_Override: PcdErrorOverrides = {
    overrides: {
        errorMessages: {
            incompleteInput: useCmsHere_IdentifierDataInsufficient,
            invalidMsaProxyTicket: useCmsHere_InvalidProxyTicket,
            expiredMsaProxyTicket: useCmsHere_ExpiredProxyTicket
        },
        targetErrorIds: {
            EmploymentStart: "employment-start-date",
            EmploymentEnd: "employment-end-date",
            Cities: "postal-address.cities",
            StreetNames: "postal-address.street-names",
            ProxyTicket: "proxy-ticket"
        },
        targetErrorMessages: {
            StreetNames: {
                invalidInput: useCmsHere_RequiredForAddress
            },
            Cities: {
                invalidInput: useCmsHere_RequiredForAddress
            }
        }
    }
};

export class PrivacySubjectDetailTypeId {
    public static readonly Demographic: DemographicSubject["kind"] = "Demographic";
    public static readonly MicrosoftEmployee: MicrosoftEmployeeSubject["kind"] = "MicrosoftEmployee";
    public static readonly MSA: MsaSelfAuthSubject["kind"] = "MSA";
}

//  Describes the fields a demographic subject can provide.
export interface DemographicSubject {
    kind: "Demographic";
    names: string[];
    emails: string[];
    phoneNumbers: string[];
    postalAddress: {
        unitNumbers: string[];
        streetNumbers: string[];
        streetNames: string[];
        cities: string[];
        regions: string[];
        postalCodes: string[];
    };
}

//  Describes the fields a Microsoft employee subject can provide.
export interface MicrosoftEmployeeSubject {
    kind: "MicrosoftEmployee";
    emails: string[];
    employeeId: string;
    employmentStartDate: Date;
    employmentEndDate: Date;
}

//  Describes the fields an MSA self auth subject can provide.
export interface MsaSelfAuthSubject {
    kind: "MSA";
    proxyTicket: string;
}

//  Types of privacy subjects.
export type PrivacySubjectIdentifier = DemographicSubject | MicrosoftEmployeeSubject | MsaSelfAuthSubject;

export interface PrivacySubjectIdentifierFormEntryComponent extends Shared.IDataEntryForm {
    //  A string representation of the identifier form type.
    identifierType: string;

    //  Gets the identifier form data input.
    getIdentifierFormData(): PrivacySubjectIdentifier;
}

//  Priority in which the manual request should be handled.
export class PrivacySubjectPriority {
    public static readonly Regular = "Regular";
    public static readonly High = "High";
}

//  Metadata passed along describing the manual request for reporting purposes.
export type ManualRequestMetadata = {
    //  CAP (Customer Assistance Portal) ID for the request being made.
    capId: string;

    //  Country of residence for the subject of the manual request.
    countryOfResidence: string;

    //  Priority in which the request should be handled.
    priority: PrivacySubjectPriority;
};

//  Information entered to act on a manual request.
export type ManualRequestEntry = {
    //  The privacy subject that requested action on their data.
    subject: PrivacySubjectIdentifier;

    //  Describes the manual request for reporting purposes.
    metadata: ManualRequestMetadata;
};

//  Privacy subject operation response.
export interface OperationResponse {
    //  List of DSR IDs.
    ids: string[];
}

export interface DataTypesOnSubjectRequest {
    demographicSubject: string[];
    microsoftEmployeeSubject: string[];
    msaSelfAuthSubject: string[];
}

export interface RequestStatus {
    context: string;
    destinationUri: string;
    id: string;
    state: "submitted" | "completed";
    subjectType: string;
    submittedTime: string;
    completedTime?: string;
    progress?: number;
}

export enum PrivacyRequestType {
    None = 0,
    Delete = 1,
    Export = 2,
    AccountClose = 3
}

export interface PrivacySubjectSelectorParent {
    //  Triggers a state of change for the component if the privacy subject is changed.
    privacySubjectChanged: () => void;
}
