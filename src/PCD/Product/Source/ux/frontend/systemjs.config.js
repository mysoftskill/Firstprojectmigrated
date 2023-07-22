/**
 * Initializes systemjs.
 */
(function (global) {
    var appVersion = global.g_AppVersion || "0.0.0.0";

    System.config({
        paths: {
            "npm:": "node_modules/",
            "app/": "app/" + appVersion + "/"
        },

        warnings: true,

        // map tells the System loader where to look for things
        map: {
            "angular": "npm:angular/angular.min.js",
            "angular-cookies": "npm:angular-cookies/angular-cookies.min.js",
            "angular-mocks": "npm:angular-mocks/angular-mocks.js",
            "angular-sanitize": "npm:angular-sanitize/angular-sanitize.min.js",
            "angular-ui-router": "npm:@uirouter/angularjs/release/angular-ui-router.min.js",
            "text": "npm:systemjs-plugin-text/text.js",
            "mee-oneui-angular": "npm:@mee/oneui.angular/dist/oneui/public/oneui.angular.min.js",
            "underscore": "npm:underscore/underscore-min.js",
            "moment": "npm:moment/min/moment.min.js",
            "@azure/msal-browser": "npm:@azure/msal-browser/dist/index.cjs.js"
        },

        meta: {
            "wwwroot/mwf/1.58.6/mwf-main.umd.min.js": { format: "global", exports: "mwf" },
            "npm:underscore/underscore-min.js": { format: "global", exports: "_" }
        },

        // packages tells the System loader how to load when no filename and/or no extension
        packages: {
            ".": {
                defaultExtension: "js"
            }
        }
    });
})(this);
