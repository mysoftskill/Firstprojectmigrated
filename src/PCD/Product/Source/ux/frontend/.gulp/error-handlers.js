const gutil = require('gulp-util');
const sass = require('gulp-dart-sass');
const notifyHelpers = require(process.cwd() +'\\frontend\\.gulp\\notify-helpers');

/**
 * Gets or sets a value determining whether or not to notify
 * the user on errors using OS notifictions
 */
exports.notifyOnError = true;

let errors = {
    tsc: [],
    sass: [],
    karma: [],
};
exports.errors = errors;

/**
 * Determine whether any errors have be logged through these functions
 */
exports.hasErrors = function hasErrors() {
    return errors.tsc.length !== 0 || errors.sass.length !== 0 || errors.karma.length !== 0;
}

/**
 * Log a typescript error to the console using the Team Services format
 * and notify the user an error has occured.
 */
exports.logTscError = function logTscError(error) {
    errors.tsc.push(error);
    logVstsError(error);
    gutil.log(error.toString());
    let errorData = {
        message: (error.diagnostic == null) ? "Error" : error.diagonstic.messageText,
        sourcePath: error.relativeFilename,
        lineNumber: error.startPosition.line,
        columnNumber: error.startPosition.character,
        code: (error.diagnostic == null) ? "Error" : error.diagnostic.code
    }

    logVstsError(errorData);
    notifyError('Gulp-Typescript Error', error.message);
}

/**
 * Log a Sass error to the console using the default Sass logger,
 * and the Team Services format. Also notify the user an error has occured.
 */
exports.logSassError = function logSassError(error) {
    errors.sass.push(error);

    let errorData = {
        message: error.messageOriginal,
        sourcePath: error.relativePath,
        lineNumber: error.line,
        columnNumber: error.column,
    };

    sass.logError.call(this, error);
    logVstsError(errorData);
    notifyError('Gulp-Sass Error', error.message);
}

/**
 * Handle a Karma server exiting and return an error to the Gulp done function
 * if the Karma server exited with a non-zero exit code (most likely meaning some
 * tests have failed)
 */
exports.errorOnKarmaExit = function errorOnKarmaExit(done, taskName) {
    return function karmaExitHandler(exitCode) {
        let error;
        if (exitCode !== 0) {
            error = new Error(`${taskName}: Karma exited with non-success exit code: ${exitCode}`);
            errors.karma.push(error);
        }

        // Pass error into done so that gulp exits with a non-zero exit code stops
        // processing other tasks
        done(error);
    }
}

/**
 * Log if a Karma component (e.g. server, runner) has exited with a non-zero exit code
 * but don't return an error to Gulp so that gulp continues to run without error (e.g.
 * during watch tasks we don't want gulp to quit upon error)
 */
exports.logKarmaExit = function logKarmaExit(done, taskName) {
    return function karmaExitLogger(exitCode) {
        if (exitCode && exitCode !== 0) {
            let message = `${taskName}: Karma exited with non-success exit code: ${exitCode}`;
            gutil.log(gutil.colors.red(message));
            errors.karma.push(new Error(message));
        } else {
            let message = `${taskName}: Karma exited with success.`;
            gutil.log(gutil.colors.green(message));
            errors.karma.pop();
        }

        // Do not pass an error into done so that gulp doesn't fail
        // on karma exit and stop watching files (assuming this is called
        // in the gulp watch task)
        done && done();
    } 
}

function notifyError(title, message) {
    if (exports.notifyOnError) {
        notifyHelpers.notifyMessage(
            gutil.colors.stripColor(title), 
            gutil.colors.stripColor(message));
    }
}

function logVstsError(errorData) {
    gutil.log(gutil.colors.red(generateVsoLogHeader(errorData)) + errorData.message);
}

/** Generate a log message in the special format that VSTS understands so errors are reported in their UI */
function generateVsoLogHeader(options) {
    // For vso, emit errors following the format here:
    // https://github.com/Microsoft/vsts-tasks/blob/master/docs/authoring/commands.md
    // Example: console.error("##vso[task.logissue type=error;sourcepath=consoleapp/main.cs;linenumber=1;columnnumber=1;code=100;]this is an error");
    options = options || {};
    
    let params = `type=${options.type || 'error'};`;
    params += options.sourcePath ? `sourcepath=${options.sourcePath};` : '';
    params += options.lineNumber ? `linenumber=${options.lineNumber};` : '';
    params += options.columnNumber ? `columnnumber=${options.columnNumber};` : '';
    params += options.code ? `code=${options.code};`: '';

    return `##vso[task.logissue ${params}]`;
}
