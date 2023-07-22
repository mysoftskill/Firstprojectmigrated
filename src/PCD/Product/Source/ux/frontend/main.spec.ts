import { SpyCache } from "./shared-tests/spec.base";
import { Main, IUntestableMain } from "./main";
import * as msal from "@azure/msal-browser";

describe("Main", () => {
    let main: Main;
    let mainMock: SpyCache<IUntestableMain>;

    let authContext: msal.PublicClientApplication;
    let authContextMock: SpyCache<msal.PublicClientApplication>;

    beforeEach(() => {
        main = new Main();
        mainMock = new SpyCache(main.getUntestableMainForMocking());

        authContext = main.getUntestableMainForMocking().createAuthContext("AppId");
        authContextMock = new SpyCache(authContext);

        mainMock.getFor("configureTelemetry").and.stub();
    });

    it("does not start app and calls login first", () => {
        // arrange
        authContextMock.getFor("loginRedirect").and.stub();

        // act
        main.bootstrapApp({
            azureAdAppId: "AppId",
            jsllAppId: "AppId",
            i9nMode: false,
            allowMocks: true,
            environmentType: null,
            preLoadedCmsContentItems: {}
        });

        // assert
        expect(mainMock.getFor("configureTelemetry")).toHaveBeenCalled();
        expect(authContextMock.getFor("loginRedirect")).toHaveBeenCalled();
        expect(mainMock.getFor("startApp")).not.toHaveBeenCalled();
    });

    it("does not start app and processes auth callback after login", () => {
        // arrange
        authContextMock.getFor("handleRedirectPromise").and.returnValue(true);

        // act
        main.bootstrapApp({
            azureAdAppId: "AppId",
            jsllAppId: "AppId",
            i9nMode: false,
            allowMocks: true,
            environmentType: null,
            preLoadedCmsContentItems: {}
        });

        // assert
        expect(mainMock.getFor("configureTelemetry")).toHaveBeenCalled();
        expect(authContextMock.getFor("handleRedirectPromise")).toHaveBeenCalled();
        expect(mainMock.getFor("startApp")).not.toHaveBeenCalled();
    });

    it("starts app once it gets the auth token", () => {
        // arrange
        mainMock.getFor("startApp").and.stub();
        authContextMock.getFor("getTokenCache").and.returnValue("AuthToken");

        // act
        main.bootstrapApp({
            azureAdAppId: "AppId",
            jsllAppId: "AppId",
            i9nMode: false,
            allowMocks: true,
            environmentType: null,
            preLoadedCmsContentItems: {}
        });

        // assert
        expect(mainMock.getFor("configureTelemetry")).toHaveBeenCalled();
        expect(authContextMock.getFor("loginRedirect")).not.toHaveBeenCalled();
        expect(mainMock.getFor("startApp")).toHaveBeenCalled();
    });

    it("starts app without auth token if it is in i9n mode", () => {
        // arrange
        mainMock.getFor("startApp").and.stub();

        // act
        main.bootstrapApp({
            azureAdAppId: "AppId",
            jsllAppId: "AppId",
            i9nMode: true,
            allowMocks: true,
            environmentType: null,
            preLoadedCmsContentItems: {}
        });

        // assert
        expect(mainMock.getFor("configureTelemetry")).toHaveBeenCalled();
        expect(authContextMock.getFor("loginRedirect")).not.toHaveBeenCalled();
        expect(mainMock.getFor("startApp")).toHaveBeenCalled();
    });
});
