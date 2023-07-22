@echo off

setlocal EnableDelayedExpansion

pushd "%~dp0\..\Source"
call ..\Build\buildenv.cmd


rem Run .NET tests
for /d %%t in (*.tests) do (
	
    pushd %%t
    dotnet test --no-build --configuration Release -l trx
    popd
    if !errorlevel! NEQ 0 goto :end
)

:end
popd
exit /b %errorlevel%
