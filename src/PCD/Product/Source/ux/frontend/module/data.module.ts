import * as angular from "angular";

//  Defines application configuration object.
export type AppConfig = {
    //  Azure AD application ID.
    azureAdAppId: string;

    //  Indicates whether the mocks are allowed.
    allowMocks: boolean;

    // TODO Define proper letter casing for PPE/PROD if deployed, then use string literal type.
    //  The environment type (e.g., int, ppe, prod).
    environmentType: string;

    //  Provides information about NGP lockdown.
    lockdown?: LockdownOptions;

    //  Mode of application.
    mode: Mode;

    //  List of behaviors that alter application mode.
    behaviors: Behavior[];
};

//  Information about NGP lockdown.
export type LockdownOptions = {
    //  Indicates whether the lockdown is active.
    isActive: boolean;

    //  Specifies when lockdown has started (UTC timestamp).
    startedUtc: string;

    //  Specifies when lockdown has ended (UTC timestamp).
    endedUtc: string;
};

//  Modes of application.
export type Mode = "i9n" | "normal";

/**
 * Behaviors of application. Use this to slightly modify behavior of the application
 * in one of the supported modes.
 */
export type Behavior = "disable-automatic-flight-discovery";

//  Data module instance.
export let dataModule = angular.module("pdmsAppData", []);
