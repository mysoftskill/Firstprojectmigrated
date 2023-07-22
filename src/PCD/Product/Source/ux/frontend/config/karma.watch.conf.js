// Karma configuration
// Generated on Tue Aug 16 2016 11:33:05 GMT-0700 (Pacific Daylight Time)

module.exports = function (config) {

    // Load default config
    var defaultConfig = require("./karma.conf");
    defaultConfig(config);

    config.singleRun = false;
    config.autoWatch = true;
    config.reporters = ["kjhtml"];
    config.logLevel = config.LOG_ERROR;
    config.browserConsoleLogOptions = "debug";
};
