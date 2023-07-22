/// <reference path="./types.d.ts"/>
import * as msal from "@azure/msal-browser";

export function initializeMeControl(user: msal.AccountInfo): void {
    
    if (window.msCommonShell) {
        let shellOptions = createShellOptions(user);
        window.msCommonShell.load(shellOptions);

        safelyResetMeControlLinks();
    } else {
        // Load the me control once msCommonShell is ready
        window.onShellReadyToLoad = () => {
            window.onShellReadyToLoad = null;

            let shellOptions = createShellOptions(user);
            window.msCommonShell.load(shellOptions);

            safelyResetMeControlLinks();
        };
    }
}

function createShellOptions(user: msal.AccountInfo): any {
    return {
        meControlOptions: {
            rpData:
            {
                msaInfo: {
                    signInUrl: window.location.href,
                    // TODO: Need to change the signoutUrl to be the "sign out page" 
                    //       of PCD eventually.
                    signOutUrl: "https://aka.ms/ngphome"
                }
            },
            userData: {
                idp: window.msCommonShell.SupportedAuthIdp.AAD,
                firstName: user.name,
                memberName: user.username,
                authenticatedState: window.msCommonShell.AuthState.SignedIn
            }
        }
    };
}

function safelyResetMeControlLinks(): void {
    if (window.MSA && window.MSA.MeControl && window.MSA.MeControl.API) {
        window.MSA.MeControl.API.setExtensibleLinks([]);
    }
}
