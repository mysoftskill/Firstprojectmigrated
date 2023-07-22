# This pulls the latest release of chromedriver to match the latest version of chrome
# cwd should be Product\Build, called by restorall and restore_devbox
pushd node_modules\protractor\node_modules\webdriver-manager\selenium
$webrequest = Invoke-WebRequest -Uri "https://chromedriver.storage.googleapis.com/LATEST_RELEASE" -UseBasicParsing
$url = "https://chromedriver.storage.googleapis.com/" + $webrequest.content + "/chromedriver_win32.zip"
$outfile = "chromedriver_" + $webrequest.content + ".zip"
wget $url -outfile $outfile
popd
pushd node_modules\.bin
$arguement = "--versions.chrome=" + $webrequest.content
.\webdriver-manager update $arguement
popd
