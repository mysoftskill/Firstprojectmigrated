const gulp = require("gulp");
const path = require("path");
const concat = require("gulp-concat");
const rename = require("gulp-rename");
const filter = require("gulp-filter");
const tsc = require("gulp-typescript");
const ts2 = require("typescript");
const terser = require('gulp-terser');
const sass = require("gulp-dart-sass");
const cleanCss = require("gulp-clean-css");
const htmlMin = require("gulp-htmlmin");
const sourcemaps = require("gulp-sourcemaps");
const del = require("del");
const merge = require("merge2");
const tslint = require("gulp-tslint");
const errorHandlers = require(process.cwd() +"\\frontend\\.gulp\\error-handlers");

errorHandlers.notifyOnError = false;

let config = {
    paths: {
        srcBase: "./frontend/",

        outDirBase: "./wwwroot/",
        outDirCode: "./wwwroot/js/",
        outDirCss: "./wwwroot/css/",

        testReports: "./test-reports/"
    }
};

let srcTsProject = tsc.createProject(path.join(config.paths.srcBase, "tsconfig.json"), {
    typescript: ts2
});

//  Cleans build output.
gulp.task("clean", () => {
    return del([
        config.paths.outDirCode,
        config.paths.outDirCss,
        config.paths.testReports,
        "!bower_components/**/*",
        "!node_modules/**/*"
    ]);
});

/**
 *  Run lint on TS source code. 
 **/
gulp.task("lint:ts", () =>
    gulp.src(config.paths.srcBase + "**/*.ts")
        .pipe(tslint({
            formatter: "stylish",
            configuration: config.paths.srcBase + "tslint.json",
            summarizeFailureOutput: true
        }))
        .pipe(tslint.report())
);

/**
 *  Compile source code. 
 **/
gulp.task("build:ts", () => {
    let tsResult = srcTsProject.src()
        .pipe(sourcemaps.init())
        .pipe(srcTsProject())
        .on("error", errorHandlers.logTscError);

    return merge([
        //  Compile typescript.
        tsResult.js
            // Add source maps for unminified source to the stream
            .pipe(sourcemaps.write("."))

            // Write unminified source and source maps to disk
            .pipe(gulp.dest(config.paths.outDirCode))

            // Filter out source maps from the stream
            .pipe(filter("**/*.js"))

            // Minify and rename source
            .pipe(terser())
            .pipe(rename({ extname: ".min.js" }))

            // Add source maps for minified source to the stream
            .pipe(sourcemaps.write("."))

            // Write minified source and source maps to disk
            .pipe(gulp.dest(config.paths.outDirCode)),

        //  Add systemjs config.
        gulp.src([config.paths.srcBase + "**/*.js", "!" + config.paths.srcBase + ".gulp/**/*.*"])
            .pipe(terser())
            .pipe(gulp.dest(config.paths.outDirCode))
    ]);
});

gulp.task("build:html", () => {
    return gulp.src(config.paths.srcBase + "**/*.html")
        .pipe(htmlMin({
            collapseWhitespace: true,
            conservativeCollapse: true,
            removeComments: true
        }))
        .pipe(gulp.dest(config.paths.outDirCode));
});

// Complile SASS. 
gulp.task("build:sass", () => {
    return gulp.src(config.paths.srcBase + "**/*.scss")
        .pipe(sourcemaps.init())

        .pipe(sass().on("error", errorHandlers.logSassError))

        // Compile sass and concat the resulting css into one file
        // The sass compiler only compiles individual streams (i.e. files)
        // into individual css files, so we manually concat the resulting
        // css files here since we want our build to produce only one css file
        .pipe(concat("site.css"))

        // Add source maps for the unminified css file to the stream
        .pipe(sourcemaps.write("."))

        // Write unminified source and source map to disk
        .pipe(gulp.dest(config.paths.outDirCss))

        // Filter out the source maps from the stream
        .pipe(filter("**/*.css"))

        // Minify Sass and change extension
        .pipe(cleanCss())
        .pipe(rename({ extname: ".min.css" }))

        // Write source maps for the minified source to the stream
        .pipe(sourcemaps.write("."))

        // Write minified source and source map to disk
        .pipe(gulp.dest(config.paths.outDirCss));
});

// Bundles telemetry. 
gulp.task("build:bradbury", () => {
    let bradburyRoot = config.paths.outDirBase + "bradbury/lib/";
    return gulp.src([bradburyRoot + "jsll.js", bradburyRoot + "bradbury-lib.js"])
        .pipe(sourcemaps.init())
        .pipe(concat("telemetry.min.js"))
        .pipe(terser())
        .pipe(sourcemaps.write("."))
        .pipe(gulp.dest(config.paths.outDirCode + "lib/"));
});

// Watch source, test, doc files and recompile them on change
gulp.task("build:watch", () => {
    gulp.watch(["**/*.ts", "**/*.js"], { cwd: config.paths.srcBase }, gulp.series("build:ts"));
    gulp.watch("**/*.html", { cwd: config.paths.srcBase }, gulp.series("build:html"));
    gulp.watch("**/*.scss", { cwd: config.paths.srcBase }, gulp.series("build:sass"));
});

/**
 * Builds all the files for this project and logs errors to the console
 */
gulp.task("build", gulp.parallel("lint:ts", "build:ts", "build:sass", "build:html", "build:bradbury"));

/**
 * Cleans output and builds all source code.
 */
gulp.task("rebuild", gulp.series("clean", "build"));
