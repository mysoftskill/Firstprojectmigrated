// Karma configuration
// Generated on Tue Aug 16 2016 11:33:05 GMT-0700 (Pacific Daylight Time)
// https://karma-runner.github.io/2.0/config/configuration-file.html

module.exports = function (config) {
    config.set({

        // base path that will be used to resolve all patterns (eg. files, exclude)
        basePath: "../../",

        // frameworks to use
        // available frameworks: https://npmjs.org/browse/keyword/karma-adapter
        frameworks: ["systemjs", "jasmine"],

        systemjs: {
            configFile: "frontend/systemjs.config.js",
            config: {
                transpiler: null,
                paths: {
                    "systemjs": "node_modules/systemjs/dist/system.src.js"
                },
                defaultJSExtensions: true,
                testFileSuffix: ".spec.js"
            }
        },

        // list of files / patterns to load in the browser
        files: [
            { pattern: "wwwroot/mwf/1.58.6/mwf-main.umd.min.js" },
            { pattern: "node_modules/jquery/dist/jquery.min.js" },
            { pattern: "node_modules/moment/min/moment.min.js" },
            { pattern: "wwwroot/bradbury/lib/bradbury-lib.js" },

            "wwwroot/js/*.spec.js",
            "wwwroot/js/**/*.spec.js",

            // Add these files to this list so Karma will serve them to the web browser and
            // setting `included` to `false` so they aren't added as script tags to the page
            { pattern: "wwwroot/**/*.*", included: false },
            { pattern: "node_modules/systemjs-plugin-text/*.*", included: false },
            { pattern: "node_modules/angular/*.*", included: false },
            { pattern: "node_modules/@azure/*.*", included: false },
            { pattern: "node_modules/angular-cookies/*.*", included: false },
            { pattern: "node_modules/angular-mocks/*.*", included: false },
            { pattern: "node_modules/angular-sanitize/*.*", included: false },
            { pattern: "node_modules/@uirouter/angularjs/release/*.*", included: false },
            { pattern: "node_modules/systemjs/dist/*.*", included: false },
            { pattern: "node_modules/underscore/*.*", included: false },
            { pattern: "node_modules/@mee/oneui.angular/dist/oneui/public/*.*", included: false }
        ],

        proxies: {
        },

        // list of files to exclude
        exclude: [
        ],

        // preprocess matching files before serving them to the browser
        // available preprocessors: https://npmjs.org/browse/keyword/karma-preprocessor
        // preprocessor config: https://karma-runner.github.io/3.0/config/configuration-file.html
        // NOTE: CMS code is excluded from code coverage. Undo, if brought back up.
        preprocessors: {
            "wwwroot/js/**/!(*.spec|*-types|*.spec.i9n|*.conf|*.tasks|*-data-mock.service|*.bootstrap|cms-*|*.min).js": ["coverage"]
        },

        // test results reporter to use.
        // available reporters: https://npmjs.org/browse/keyword/karma-reporter
        // "junit" reporter is used so that it outputs "junit.xml" for build agents to consume.
        reporters: ["junit", "progress"],

        // configure how the browser console is logged
        browserConsoleLogOptions: "error",

        // web server port
        port: 9876,

        // enable / disable colors in the output (reporters and logs)
        colors: true,

        // level of logging
        // possible values: config.LOG_DISABLE || config.LOG_ERROR || config.LOG_WARN || config.LOG_INFO || config.LOG_DEBUG
        logLevel: config.LOG_ERROR,

        // enable / disable watching file and executing tests whenever any file changes
        autoWatch: false,

        // start these browsers
        // available browser launchers: https://npmjs.org/browse/keyword/karma-launcher
        browsers: ["ChromeHeadless"],

        // Continuous Integration mode
        // if true, Karma captures browsers, runs the tests and exits
        singleRun: true,

        // Concurrency level
        // how many browser should be started simultaneous
        concurrency: Infinity,

        // junit configuration
        junitReporter: {
            outputDir: "test-reports/unit-tests", // results will be saved as $outputDir/$browserName.xml
            outputFile: "junit.xml", // if included, results will be saved as $outputDir/$browserName/$outputFile
            suite: "Karma Unit Tests",
        },

        phantomjsLauncher: {
            options: {
                // Set log_level to DEBUG to view full error stack traces
                onError: function (msg, trace) {
                    var msgStack = ["ERROR: " + msg];
                    if (trace && trace.length) {
                        msgStack.push("TRACE:");
                        trace.forEach(function (t) {
                            msgStack.push(" -> " + t.file + ": " + t.line + (t.function ? " (in function '" + t.function + "')" : ""));
                        });
                    }
                    console.log(msgStack.join("\n"));
                }
            }
        },

        coverageReporter: {
            // specify a common output directory
            dir: "test-reports/unit-tests/coverage",
            check: {
                // These values are derived based on what the current state of unit test coverage is.
                // If you drop below the coverage, build will fail. Please add more unit tests to pass coverage.
                global: {
                    statements: 75,
                    lines: 78,
                    functions: 65,
                    branches: 66
                }
            },
            reporters: [
                // reporters supporting the `file` property, use `subdir` to directly
                // output them in the `dir` directory
                { type: "cobertura", subdir: ".", file: "tests-coverage.xml" },
                { type: "text", subdir: ".", file: "tests-coverage.txt" },
                { type: "text-summary" }
            ]
        },

        remapIstanbulReporter: {
            reports: {
                html: "test-reports/unit-tests/coverage"
            }
        }
    });
};
