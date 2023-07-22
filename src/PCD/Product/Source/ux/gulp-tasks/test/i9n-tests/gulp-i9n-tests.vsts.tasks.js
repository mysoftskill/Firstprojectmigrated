require(process.cwd() + "\\gulp-tasks\\test\\i9n-tests\\gulp-i9n-tests.tasks");

const gulp = require("gulp");
const protractor = require("gulp-protractor").protractor;

let config = {
    paths: {
        outDirCode: "./wwwroot/js/"
    }
};

/**
 *  Start Protractor for VSTS. '
 **/
gulp.task("protractor:start:vsts", (done) => {
    gulp.src([config.paths.outDirCode + "i9n-tests/**/*.spec.i9n.js"])
        .pipe(protractor({
            configFile: "./frontend/config/protractor.vsts.conf.js"
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
