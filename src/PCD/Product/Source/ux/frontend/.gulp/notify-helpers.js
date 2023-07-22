const path = require('path');
const notifier = require('node-notifier');

const iconPath = path.join(__dirname, 'gulp-error.png');

exports.notifyMessage = function(title, message) {
    notifier.notify({
        title: title,
        message: message,
        icon: iconPath,
        sound: false
    });
}