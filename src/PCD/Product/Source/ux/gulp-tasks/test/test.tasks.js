require(process.cwd() + "\\gulp-tasks\\test\\unit-tests\\gulp-unit-tests.tasks");
require(process.cwd() + "\\gulp-tasks\\test\\unit-tests\\gulp-unit-tests.vsts.tasks");
require(process.cwd() + "\\gulp-tasks\\test\\i9n-tests\\gulp-i9n-tests.tasks");
require(process.cwd() + "\\gulp-tasks\\test\\i9n-tests\\gulp-i9n-tests.vsts.tasks");

const gulp = require("gulp");

/**
 * Run all tests
 */
gulp.task("test", gulp.parallel("test:unit", "test:i9n"));
