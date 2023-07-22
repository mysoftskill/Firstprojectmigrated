# This script set up all necessary dependencies in order to run the integration tests on CloudTest
# It then executes these tests
write-host "`n----------------------------"
write-host " Download node msi  "
write-host "----------------------------`n"
$version = "12.13.0-x64"
$url = "https://nodejs.org/dist/v12.13.0/node-v12.13.0-x64.msi"
$filename = "node.msi"
$node_msi = $env:WorkingDirectory + "\$filename"
$wc = New-Object System.Net.WebClient
$wc.DownloadFile($url, $node_msi)
write-Output "$filename downloaded"
write-host "`n----------------------------"
write-host " nodejs installation  "
write-host "----------------------------`n"
write-host "[NODE] running $node_msi"
Start-Process $node_msi -Wait

$env:Path += ";C:\Program Files\nodejs\"
write-host "`n----------------------------"
write-host " Checking Environment Variables and node version"
write-host "----------------------------`n"
$env:NODE_PATH = $env:WorkingDirectory + "\intTest\node_modules"
Get-ChildItem Env:
npm -v
node -v
#Replace PAT credentials
write-host "`n----------------------------"
write-host " Replacing PAT credentials"
write-host "----------------------------`n"
$outputFileScriptLoc = $env:WorkingDirectory +"\intTest\CloudTest"
pushd $outputFileScriptLoc
.\keyvaultPatInstaller.ps1
popd

#Remove node_modules
write-host "`n----------------------------"
write-host " Installing Npm dependencies and setting current directory"
write-host "----------------------------`n"
pushd $env:WorkingDirectory
pushd intTest
npm install

npm run webdriver-update
#Updates chromedriver to include newest version
write-host "`n----------------------------"
write-host "updating to include newest chromedriver version"
write-host "----------------------------`n"
#CWD needs to be WorkingDirectory\intTest
#This script is copied from the Product\Build folder as part of the build artifacts
.\latest_chromedriver.ps1
write-host "`n----------------------------"
write-host "Convert key files from symbolic links to actual files"
write-host "----------------------------`n"
#Symbolic links changes the absolute path that npm uses to load the webserver
$wwwroot_mwf = $env:WorkingDirectory + "\intTest\wwwroot\mwf\1.58.6"
pushd $wwwroot_mwf
Get-ChildItem | Where-Object {$_.Attributes -match "ReparsePoint"} | ForEach-Object { Copy-Item $_ holdercopy$_}
Get-ChildItem | Where-Object {$_.Attributes -match "ReparsePoint"} | ForEach-Object { del $_}
Get-ChildItem | Where-Object {$_.Name.StartsWith("holdercopy")} | ForEach-Object { $_ | Rename-Item -NewName $_.Name.Replace('holdercopy','')}
popd
write-host "`n----------------------------"
write-host "Installing chrome"
write-host "----------------------------`n"
$chromeInstallFile= $env:WorkingDirectory + "\intTest\CloudTest\install_chrome.ps1"
powershell -File $chromeInstallFile
write-host "`n----------------------------"
write-host "Trusting dev cert"
write-host "----------------------------`n"
dotnet dev-certs https --trust
write-host "`n----------------------------"
write-host "Launching ux.exe"
write-host "----------------------------`n"
Start-Process -FilePath "ux.exe" -argument "--i9nMode"
write-host "`n----------------------------"
write-host "running protractor files"
write-host "----------------------------`n"

npm run gulp clean
npm run gulp build
# The first run initialized the test, which loads the page into the cache, and doesn't affect the jasmine timeout
# This prevents the async callback error on the first couple testcases
npm run gulp protractor:start
npm run gulp protractor:start

write-host "`n----------------------------"
write-host "Copying testing file to output location"
write-host "----------------------------`n"
$outputFileScriptLoc = $env:WorkingDirectory +"\intTest\CloudTest"
pushd $outputFileScriptLoc
.\Converter\ConvertJunitToNunit.ps1
