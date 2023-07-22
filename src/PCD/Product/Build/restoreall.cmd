@echo off

pushd "%~dp0\..\.."

call Product\Build\buildenv.cmd

rem Restore .NET dependencies
rem First restore is for updated solutions
rem Second is for older solutions, this is because OneBranch image doesn't have nuget version 4.7.1.5393 
nuget restore pdmsux.sln
Product\Build\tools\nuget\nuget restore pdmsux.sln
if %errorlevel% NEQ 0 goto :end

rem stores the version of node in nodeversion
echo on 
node -v > tmpFile
set /p nodeversion= < tmpFile
del tmpFile

rem Restore frontend dependencies
pushd Product\Source\ux

rem Do not authenticate to the CDPx pipeline this is done in the YAML Files
rem However, if you are running this on a devbox, the npm authentication commands will need to be added

rem Install all packages.
::call npm config set scripts-prepend-node-path auto
call npm install --no-save --no-audit
if %errorlevel% NEQ 0 goto :endfrontend

rem Update webdriver-manager for i9n testing.
call npm run webdriver-update
if %errorlevel% NEQ 0 goto :endfrontend

rem hard sets version of chromedriver, trouble determining npm dependencies
powershell -File "%dp0\..\..\..\Build\latest_chromedriver.ps1"

rem clean up webdriver recursive filename bug
if EXIST node_modules\webdriver-js-extender\built\built\built (
    rmdir /S /Q node_modules\webdriver-js-extender\built\built\built 
) ELSE (
    echo "Webdriver package has already been cleaned""
)

:endfrontend
popd
goto :end

:end
popd
exit /b %errorlevel%
