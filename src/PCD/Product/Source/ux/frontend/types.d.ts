/// <reference path="./underscore.d.ts"/>
/// <reference path="../node_modules/@types/jquery/index.d.ts"/>
/// <reference path="../wwwroot/bradbury/lib/bradbury-lib.d.ts"/>
/// <reference path="../node_modules/@mee/oneui.angular/dist/oneui/public/oneui.angular.d.ts"/>

interface Window {
    msCommonShell: any;
    onShellReadyToLoad: any;
    MSA: any;
}

declare module "*!text" {
    const textExport: string;
    export = textExport;
}
