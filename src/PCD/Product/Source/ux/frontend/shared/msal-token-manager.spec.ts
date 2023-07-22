import * as angular from "angular";
import * as msal from "@azure/msal-browser";
import { TestSpec, SpyCache } from "../shared-tests/spec.base";

import { IMsalTokenManager, MsalTokenManager, MsalTokenManagerOptions } from "./msal-token-manager";

describe("MSAL token manager", () => {
    let spec: TestSpec;
    let mockAuthContext: Partial<msal.PublicClientApplication>;
    let spy: SpyCache<Partial<msal.PublicClientApplication>>;

    beforeEach(() => {
        spec = new TestSpec();

        mockAuthContext = <Partial<msal.PublicClientApplication>> { acquireToken() { } };
        spy = new SpyCache(mockAuthContext);
        spy.getFor("acquireTokenSilent").and.stub();
    });

    it("acquires the token using the msal authentication context specified on initialization", () => {
        let msalTokenManagerOptions: MsalTokenManagerOptions = {
            $q: spec.$promises,
            authCtx: <msal.PublicClientApplication> mockAuthContext,
            resource: null,
        };
        let msalTokenManager = new MsalTokenManager(msalTokenManagerOptions);
        msalTokenManager.getTokenAsync();
        spec.runDigestCycle();

        expect(mockAuthContext.acquireTokenSilent).toHaveBeenCalled();
    });

    it("acquires the token for the resource specified on initialization", () => {
        let expectedResource = "TestResource";

        let msalTokenManagerOptions: MsalTokenManagerOptions = {
            $q: spec.$promises,
            authCtx: <msal.PublicClientApplication> mockAuthContext,
            resource: expectedResource,
        };
        let msalTokenManager = new MsalTokenManager(msalTokenManagerOptions);
        msalTokenManager.getTokenAsync();
        spec.runDigestCycle();

        // Check the first argument of the first call to acquireToken
        expect(spy.getFor("acquireTokenSilent").calls.argsFor(0)[0]).toBe(expectedResource);
    });

    it("resolved the promise when the token is successfully acquired", (done: DoneFn) => {
        let expectedToken = "Acquired Token";

        let msalTokenManagerOptions: MsalTokenManagerOptions = {
            $q: spec.$promises,
            authCtx: <msal.PublicClientApplication> mockAuthContext,
            resource: null,
        };
        let msalTokenManager = new MsalTokenManager(msalTokenManagerOptions);
        let tokenPromise = msalTokenManager.getTokenAsync();
        // Fake a success callback
        spy.getFor("acquireTokenSilent").calls.argsFor(0)[1].call(null, "", expectedToken, "");
        tokenPromise.then((token) => {
            expect(token).toBe(expectedToken);
            done();
        });
        spec.runDigestCycle();
    });

    it("rejects the promise when the token is not successfully acquired", (done: DoneFn) => {
        let expectedMessage = "No token for resource";

        let msalTokenManagerOptions: MsalTokenManagerOptions = {
            $q: spec.$promises,
            authCtx: <msal.PublicClientApplication> mockAuthContext,
            resource: null,
        };
        let msalTokenManager = new MsalTokenManager(msalTokenManagerOptions);
        let tokenPromise = msalTokenManager.getTokenAsync();
        // Fake a failure callback (no token)
        spy.getFor("acquireTokenSilent").calls.argsFor(0)[1].call(null, expectedMessage, "", "");
        tokenPromise.catch((message: string) => {
            expect(message).toBe(expectedMessage);
            done();
        });
        spec.runDigestCycle();
    });
});