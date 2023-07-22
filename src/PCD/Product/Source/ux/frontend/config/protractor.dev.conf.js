exports.config = {
    // For directly connection to local webdriver instance.
    directConnect: true,

    // Base URL for i9n mode.
    baseUrl: "https://localhost:5000/",

    // Capabilities to be passed to the webdriver instance.
    capabilities: {
        // Chrome options.
        browserName: "chrome",
        chromeOptions: {
            args: ["--allow-insecure-localhost", "--window-size=1480,1080", "--auto-open-devtools-for-tabs", "--no-sandbox", "--enable-automation"]
        }
    },

    // Jasmine options.
    framework: "jasmine",
    jasmineNodeOpts: {
        showColors: true,
        defaultTimeoutInterval: 10000 // msec
    },

    logLevel: "DEBUG",

    onPrepare: function () {
        // TODO: Add coverage reporting eventually.
        var specReporter = require("jasmine-spec-reporter").SpecReporter;
        jasmine.getEnv().addReporter(new specReporter({
            spec: {
                // same as "local" config.
                displaySuccessful: true,
                displayFailed: true,
                displayPending: true,
                displayDuration: true,

                // different from "local" config.
                displayErrorMessages: true
            },
            colors: {
                enabled: true
            }
        }));
    }
};
