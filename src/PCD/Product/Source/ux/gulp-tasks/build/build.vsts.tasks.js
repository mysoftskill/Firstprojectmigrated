require(process.cwd() + "\\gulp-tasks\\build\\build.tasks");

const gulp = require("gulp");
const errorHandlers = require(process.cwd() + "\\frontend/.gulp/error-handlers");
errorHandlers.notifyOnError = false;

/**
 * Builds the source, logs errors to the console, and if errors occur, exits gulp
 * with a non-success error code so Team Services views this build step as failing
 */
gulp.task("vsts:build", gulp.series("rebuild", (done) => {
    if (errorHandlers.hasErrors()) {
        // Process must exit with non-zero status code for build to fail in VSTS
        throw new Error("Errors occurred in the build. See above logs for details.");
    }
    done();
}));
