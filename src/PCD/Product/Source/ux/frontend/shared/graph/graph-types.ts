//  Represents Resource.
export interface GraphResource {
    //  ID.
    id: string;

    //  Display name.
    displayName: string;

    //  Is this resource invalid. i.e. is it known to Pdms and not to Graph
    isInvalid: boolean;
}

//  Represents Group.
export interface Group extends GraphResource {
    //  Email
    email: string;

    //  true if it is a security group
    securityEnabled: boolean;
}

//  Represents User.
export interface User extends GraphResource {
    //  Email
    email: string;
}

/** Represents Contact. A Contact is a Resource which has an email associated with it.
    This could be a User or Distribution Group or a mail enabled Security Group
*/
export interface Contact extends GraphResource {
    //  Email
    email: string;
}

export interface Application extends GraphResource { }

//*********** Graph typings. ***********
export interface GraphGroup {
    id: string;

    displayName: string;

    securityEnabled: boolean;

    mail: string;

    mailEnabled: boolean;
}

export interface GraphUser {
    id: string;

    displayName: string;

    mail: string;

    userPrincipalName: string;
}

export interface GraphApplication {
    id: string;

    displayName: string;
}

export interface GraphAjaxResponseGroupData {
    value: GraphGroup[];
}

export interface GraphAjaxResponseUserData {
    value: GraphUser[];
}

export interface GraphAjaxResponseApplicationData {
    value: GraphApplication[];
}
