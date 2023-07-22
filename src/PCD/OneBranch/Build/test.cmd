setlocal

:: set repo root folder
set REPO_ROOT_DIR=%~dp0..\..\..\..
pushd "%REPO_ROOT_DIR%"
set REPO_ROOT_DIR=%cd%
popd

if "%~1"=="" (
    set BUILD_CONFIG=Debug
) else (
    set BUILD_CONFIG=%1
)

pushd %~dp0


if exist "%REPO_ROOT_DIR%\src\PCD\Product\Source\ux.tests\TestResults" (
    del /q "%REPO_ROOT_DIR%\src\PCD\Product\Source\ux.tests\TestResults\*"
    )
:: Unit tests
call %REPO_ROOT_DIR%\src\PCD\Product\Build\testall.cmd %BUILD_CONFIG% || exit /b 1

::Integration Testing
::Unable to launch due to OneBranch network access restrictions, instead launch cloudtest
::pushd "%REPO_ROOT_DIR%\src\PCD\Product\Source\ux
::dotnet dev-certs https
::Set ASPNETCORE_ENVIRONMENT="Development"
::powershell -File "%REPO_ROOT_DIR%\src\PCD\Product\Scripts\Testing\I9nMode.ps1" -start -runTests -context "local"
::.\bin\Debug\ux.exe --i9nMode

popd

endlocal
echo Everything is awesome! Bye.
exit /b 0
