REM @echo off

setlocal EnableDelayedExpansion

if "%~1"=="" (
    set BUILD_CONFIG=Debug
) else (
    set BUILD_CONFIG=%1
)

pushd "%~dp0\..\Source"
REM call ..\Build\buildenv.cmd

rem Run .NET tests
for /d %%t in (ux.tests) do (
    pushd %%t
    dotnet test --no-build --configuration %BUILD_CONFIG% -l trx
    popd
    if !errorlevel! NEQ 0 goto :end
)

rem Run frontend unit tests
rem TODO: Combine unit tests and integration tests into one VSTS task
pushd ux
call node_modules\.bin\gulp.cmd test:unit
popd

if %errorlevel% NEQ 0 goto :end

:end
popd
exit /b %errorlevel%
