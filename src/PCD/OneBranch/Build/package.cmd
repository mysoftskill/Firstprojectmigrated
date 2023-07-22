:: Package PCF Azure Applications

setlocal

if "%~1"=="" (
    set BUILD_CONFIG=Debug
) else (
    set BUILD_CONFIG=%1
)

:: set repo root folder
set REPO_ROOT_DIR=%~dp0..\..\..\..
pushd "%REPO_ROOT_DIR%"
set REPO_ROOT_DIR=%cd%
popd

set ONEBRANCH_DIR=%REPO_ROOT_DIR%\src\PCD\OneBranch

echo Preparation steps
if exist "%ONEBRANCH_DIR%\Release" (
    rmdir /Q/S "%ONEBRANCH_DIR%\Release"
    )
xcopy "%REPO_ROOT_DIR%\src\Deployment" "%ONEBRANCH_DIR%\Release" /S /I /Q /Y /F || exit /b 1
xcopy "%REPO_ROOT_DIR%\src\Deployment\PCDGenevaSynthetics" "%ONEBRANCH_DIR%\Release" /S /I /Q /Y /F || exit /b 1
xcopy "%ONEBRANCH_DIR%\Deployment" "%ONEBRANCH_DIR%\Release" /S /I /Q /Y /F || exit /b 1
mkdir "%ONEBRANCH_DIR%\Release\Bin"

echo Building geneva synthetics agent package
call %REPO_ROOT_DIR%\.build\package_syn.cmd PCD SyntheticJob.json Product\Source\SyntheticJob\bin\%BUILD_CONFIG%\net472 || exit /b 1

:: Do this after you have a service fabric 
echo Building service fabric packages_sfapp
call %REPO_ROOT_DIR%\.build\package_sfapp.cmd PCD PcdUxApp %BUILD_CONFIG% || exit /b 1

:: TODO: Update Gma file
echo Building service fabric packages_gma
call %REPO_ROOT_DIR%\.build\package_gma.cmd PCD || exit /b 1

endlocal

echo Everything is awesome! Bye.
exit /b 0
