/// <binding BeforeBuild='build' Clean='clean' ProjectOpened='build:watch' />
///Neeed to include the absolute path due to symlink file resolution in CloudTest
///Make sure current working directory is set to Source\ux
require(process.cwd()+"\\gulp-tasks\\build\\build.tasks");
require(process.cwd() +"\\gulp-tasks\\build\\build.vsts.tasks");
require(process.cwd()+"\\gulp-tasks\\test\\test.tasks");

const gulp = require("gulp");

/** Default task that builds and runs test */
gulp.task("default", gulp.series("rebuild", "test"));
