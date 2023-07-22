:: Restore NuGet packages

setlocal

:: set repo root folder
set REPO_ROOT_DIR=%~dp0..\..\..\..
pushd "%REPO_ROOT_DIR%"
set REPO_ROOT_DIR=%cd%
popd

set SOLUTION_FILE="%REPO_ROOT_DIR%\src\PXS\Product\PrivacyExperienceSvc.sln"

echo Restore NuGet packages %SOLUTION_FILE%
nuget restore %SOLUTION_FILE% || exit /b 1
:: dotnet restore %SOLUTION_FILE% || exit /b 1

endlocal
echo Everything is awesome! Bye.
exit /b 0