:: Package PAF Azure Applications
:: %1 parameters values: PAF and AID
:: %2 parameters values: Functions and AIdFunctions
:: %3 parameters values: WorkItemProcessor and AnaheimIdProcessor

mkdir "%ONEBRANCH_DIR%\Release\%1"
mkdir "%ONEBRANCH_DIR%\Release\%1\Bin"
mkdir "%ONEBRANCH_DIR%\Release\%1\Templates"
xcopy "%REPO_ROOT_DIR%\src\Deployment\FunctionTemplates" "%ONEBRANCH_DIR%\Release\%1\Templates" /S /I /Q /Y /F || exit /b 1
xcopy "%ONEBRANCH_DIR%\Deployment\%1" "%ONEBRANCH_DIR%\Release\%1" /S /I /Q /Y /F || exit /b 1

:: Compress to zip file
set PLATFORM=x64
set SOURCE_DIR=%REPO_ROOT_DIR%\src\PAF\Product\%2\bin\%PLATFORM%\Release
set DESTINATION_DIR=%ONEBRANCH_DIR%\Release\%1\Bin
set PACKAGE_NAME=%3
set COMPRESS_PS1=%REPO_ROOT_DIR%\.build\CompressArchive.ps1

powershell "%COMPRESS_PS1%" "%SOURCE_DIR%\*" "%DESTINATION_DIR%\%PACKAGE_NAME%.zip" || exit /b 1

endlocal

echo Done packaging %1.
exit /b 0