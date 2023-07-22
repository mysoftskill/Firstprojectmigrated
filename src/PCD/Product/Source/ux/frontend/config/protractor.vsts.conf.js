const i9nTestReportLocation = "test-reports/i9n-tests";

// TODO: Disabling screenshot reporter temporarily due to unstability in build agents.
//var htmlScreenshotReporter = require("protractor-jasmine2-screenshot-reporter");
//var screenshotReporter = new htmlScreenshotReporter({
//    //  Reference for options config: https://www.npmjs.com/package/protractor-jasmine2-screenshot-reporter 
//    dest: i9nTestReportLocation,
//    filename: "failed-tests-screenshot-report.html",
//    ignoreSkippedSpecs: true,
//    captureOnlyFailedSpecs: true,
//    reportOnlyFailedSpecs: true,
//    showSummary: true,
//    showQuickLinks: true,
//    reportFailedUrl: true,
//    inlineImages: true
//});

exports.config = {
    /**
     * ===============================================
     * Config same as `protractor.conf.js`
     * ===============================================
     */
    // For directly connection to local webdriver instance.
    directConnect: true,

    // Base URL for i9n mode.
    baseUrl: "https://localhost:5000/",

    // Jasmine options.
    framework: "jasmine",
    jasmineNodeOpts: {
        showColors: true,
        defaultTimeoutInterval: 10000 // msec
    },

    logLevel: "ERROR",

    // TODO: Disabling screenshot reporter temporarily due to unstability in build agents.
    //beforeLaunch: function () {
    //    //  By default, no report is generated if an exception is thrown from within the test run.
    //    //  So we catch these errors and make jasmine report current spec explicitly.
    //    process.on("uncaughtException", function () {
    //        screenshotReporter.jasmineDone();
    //        screenshotReporter.afterLaunch();
    //    });

    //    return new Promise(function (resolve) {
    //        screenshotReporter.beforeLaunch(resolve);
    //    });
    //},

    onPrepare: function () {
        // TODO: Add coverage reporting eventually.
        var specReporter = require("jasmine-spec-reporter").SpecReporter;
        jasmine.getEnv().addReporter(new specReporter({
            spec: {
                displaySuccessful: true,
                displayFailed: true,
                displayPending: false,
                displayDuration: true
            },
            colors: {
                enabled: true
            }
        }));

        var junitXmlReporter = require("jasmine-reporters").JUnitXmlReporter;
        jasmine.getEnv().addReporter(new junitXmlReporter({
            // setup the output path for the junit reports
            savePath: i9nTestReportLocation,

            // consolidateAll when set to true:
            //   output/junitresults.xml
            //
            // consolidateAll when set to false:
            //   output/junitresults-example1.xml
            //   output/junitresults-example2.xml
            consolidateAll: true
        }));

        // TODO: Disabling screenshot reporter temporarily due to unstability in build agents.
        //jasmine.getEnv().addReporter(screenshotReporter);
    },

    // TODO: Disabling screenshot reporter temporarily due to unstability in build agents.
    //afterLaunch: function (exitCode) {
    //    return new Promise(function (resolve) {
    //        screenshotReporter.afterLaunch(resolve.bind(this, exitCode));
    //    });
    //},

    /**
     * ===============================================
     * Config different from `protractor.conf.js`
     * ===============================================
     */
    // Capabilities to be passed to the webdriver instance.
    capabilities: {
        // Chrome options.
        browserName: "chrome",
        chromeOptions: {
            args: ["--headless", "--disable-gpu", "--allow-insecure-localhost", "--window-size=900,600", "--enable-logging=true", "--no-sandbox", "--enable-automation"]
        },

        // Allows tests to run in parallel.
        // If this is set to be true, specs will be divided by file.
        // Set to `false` because file sharding causes flakiness in tests.
        shardTestFiles: false,

        // Maximum number of browser instances that can run in parallel for this
        // set of capabilities. This is only needed if shardTestFiles is true.
        // Default is 1.
        maxInstances: 20
    },

    plugins: [{
        //  Used to show browser console logs on Protractor output.
        package: "protractor-console",
        logLevels: ["severe", "warning"]
    }]
};
