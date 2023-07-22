const gulp = require("gulp");
const karma = require("karma");
const path = require("path");
const open = require("gulp-open");
const browserSync = require("browser-sync");

let config = {
    paths: {
        karmaConfig: path.join(process.cwd(), "\\frontend\\config\\karma.conf.js"),
        karmaWatchConfig: path.join(process.cwd(), "\\frontend\\config\\karma.watch.conf.js")
    }
};
require(process.cwd() + "\\frontend\\config\\karma.conf")({ set: (config) => testConfig = config || {} });

const errorHandlers = require(process.cwd() + "\\frontend\\.gulp\\error-handlers");
errorHandlers.notifyOnError = false;

/**
 *  Start a Karma server. 
 **/
gulp.task("karma:start-server", (done) => {
    // Pass in null callback as this task should not block other tasks from running
    new karma.Server({
        configFile: config.paths.karmaWatchConfig,
    }, errorHandlers.logKarmaExit(done, "karma:start-server")).start();
});

/**
 *  Start a Karma server, run unit tests, and then exit. 
 **/
gulp.task("karma:local-single-run", (done) => {
    new karma.Server({
        configFile: config.paths.karmaConfig

        // Uncomment this for debugging unit tests through console.debug 
       //logLevel: "DEBUG",
    }, errorHandlers.logKarmaExit(done, "karma:local-single-run")).start();
});

gulp.task("karma:send-run-request", (done) => {
    karma.runner.run({
        configFile: config.paths.karmaWatchConfig
    }, errorHandlers.logKarmaExit(done, "karma:send-run-request"));
});

/**
 * Run unit tests
 */
gulp.task("test:unit", gulp.series("karma:local-single-run"));


//  Watch source and test files. Then recompiles and re-runs unit tests.
gulp.task("test:unit:watch", () => {
    //  Karma will re-run tests on each detected change.
    gulp.watch("**/*.ts", { cwd: config.paths.srcBase }, gulp.series("build:ts", "karma:local-single-run"));
});

gulp.task("browser-reload", (done) => {
    browserSync.reload();
    done();
});

// Watch source and test files. Then recompiles, re-runs unit tests and reloads browser.
gulp.task("test:unit:watch-reload", () => {
    //  Karma will re-run tests on each detected change.
    gulp.watch("**/*.ts", { cwd: config.paths.srcBase }, gulp.series("build:ts", "browser-reload"));
});

/**
 * Runs unit tests on a browser, watches for changes and reloads browser.
 */
gulp.task("test:unit:dev", () => {
    browserSync.init({
        proxy: `localhost:${testConfig.port}`,
        injectChanges: false,
        reloadDelay: 0
    });

    gulp.series("build", gulp.parallel("karma:start-server", "test:unit:watch-reload"))();
});

/**
 * Shows the detailed unit tests coverage report on a browser.
 */
gulp.task("test:unit:coverage:show", (done) => {
    gulp.src("./test-reports/unit-tests/coverage/index.html")
        .pipe(open());
    done();
});

/**
 * Runs the unit test suite, updates coverage and shows coverage report on browser.
 */
gulp.task("test:unit:coverage", (done) => {
    gulp.series("test:unit", "test:unit:coverage:show")(done);
});

/**
 * Opens up the debug file for tests.
 */
gulp.task("test:unit:open", gulp.series("karma:start-server", () => {
    return gulp.src(__filename)
        .pipe(open({ uri: `https://localhost:${testConfig.port}/debug.html` }));
}));
