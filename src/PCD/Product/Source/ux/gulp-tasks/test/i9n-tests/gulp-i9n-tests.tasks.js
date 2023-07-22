const gulp = require("gulp");
const protractor = require("gulp-protractor").protractor;
const shell = require("gulp-shell");

let config = {
    paths: {
        srcBase: "./frontend/",
        outDirCode: "./wwwroot/js/"
    }
};

/**
 *  Start ux.exe in i9n mode for tests in local mode. 
 **/
gulp.task("start-i9n-mode:run-test:local", (done) => {
    gulp.src(["./"])
        .pipe(shell([
            //  The relative path resolves based on "working folder" of build task.
            "powershell -executionPolicy bypass -command \".\\CloudTest\\I9nMode.ps1 -start -runTests\""
        ]))
        .on("end", () => { done(); });
});

/**
 *  Run i9n tests. 
 **/
gulp.task("test:i9n", gulp.series("start-i9n-mode:run-test:local"));

/**
 *  Start ux.exe in i9n mode for tests in local-dev mode. 
 **/
gulp.task("start-i9n-mode:run-test:dev", (done) => {
    var psCommand = ".\\CloudTest\\I9nMode.ps1 -start -runTests -context \"local-dev\"";
    gulp.src(["./"])
        .pipe(shell([
            "powershell -executionPolicy bypass -command " + psCommand
        ]))
        .on("end", () => { done(); });
});

/**
 *  Start ux.exe in i9n mode for development. 
 **/
gulp.task("start-i9n-mode:dev", (done) => {
    var psCommand = ".\\CloudTest\\I9nMode.ps1 -start -context \"local-dev\"";
    gulp.src(["./"])
        .pipe(shell([
            "powershell -executionPolicy bypass -command " + psCommand
        ]))
        .on("end", () => { done(); });
});

/**
 * Run i9n tests for development. It starts ux.exe in i9n mode, runs tests, watches for file changes and rerun tests.
 * The tests run in headful Chrome browser. You can also access the application at https://localhost:5000
 * Note: If you are making changes in Webrole, you would need to re-build the solution manually and run `gulp test:i9n` 
 */
gulp.task("test:i9n:dev", gulp.series("start-i9n-mode:run-test:dev", () => {
    gulp.watch(["**/*.ts", "**/*.html", "**/*.scss"], { cwd: config.paths.srcBase }, gulp.series("protractor:start:dev"));
}));

/**
 *  Start Protractor. 
 **/
gulp.task("protractor:start", (done) => {
    gulp.src([config.paths.outDirCode + "i9n-tests/**/*.spec.i9n.js"])
        .pipe(protractor({
            configFile: "./frontend/config/protractor.conf.js"
        }))
        .on("error", () => {
            console.log("\nI9n tests failed. \n");
            gulp.series("stop-i9n-mode")(done);
        })
        .on("end", () => {
            console.log("\nI9n tests passed successfully. \n");
            gulp.series("stop-i9n-mode")(done);
        });
});

/**
 *  Start Protractor for development. 
 **/
gulp.task("protractor:start:dev", gulp.series("build:ts", "build:sass", "build:html", (done) => {
    gulp.src([config.paths.outDirCode + "i9n-tests/**/*.spec.i9n.js"])
        .pipe(protractor({
            configFile: "./frontend/config/protractor.dev.conf.js",
            debug: true
        }))
        .on("error", () => {
            console.log("I9n tests failed.");
            done();
        })
        .on("end", () => {
            console.log("I9n tests passed successfully.");
            done();
        });
}));

/**
 *  Stop ux.exe in i9n mode. 
 **/
gulp.task("stop-i9n-mode", (done) => {
    gulp.src(["./"])
        .pipe(shell([
            //  The relative path resolves based on "working folder" of build task.
            "powershell -executionPolicy bypass -command \".\\CloudTest\\I9nMode.ps1 -stop\""
        ]))
        .on("end", () => { done(); });
});

/**
 *  Stop ux.exe in i9n mode for development. This task is just for symmetry. 
 **/
gulp.task("stop-i9n-mode:dev", gulp.series("stop-i9n-mode"));
