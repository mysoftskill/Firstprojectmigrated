const gulp = require("gulp");
const karma = require("karma");
const path = require("path");

let config = {
    paths: {
        karmaConfig: path.join(process.cwd(), "\\frontend\\config\\karma.conf.js")
    }
};
require(process.cwd() + "\\frontend\\config\\karma.conf")({ set: (config) => testConfig = config || {} });

const errorHandlers = require(process.cwd() + "\\frontend\\.gulp\\error-handlers");
errorHandlers.notifyOnError = false;

/**
 * Start a Karma server, run unit tests, and then exit.
 * Includes special configuration for Continuous Integration runs such as
 * disabling colors in logs and exiting with a non-success error code on
 * test failure
 */
gulp.task("vsts:karma:ci", (done) => {
    // Disable colors for Continuous Integration scenarios
    new karma.Server({
        configFile: config.paths.karmaConfig,
        colors: false,
        logLevel: "DEBUG"
    }, errorHandlers.errorOnKarmaExit(done, "vsts:karma:ci")).start();
});

/**
 * Runs all unit tests, logs errors to the console, and if errors occur, exits gulp
 * with a non-success error code so Team Services views this build step as failing
 */
gulp.task("vsts:test:unit", gulp.series("vsts:karma:ci"));
