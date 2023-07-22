@echo on

pushd "%~dp0\..\.."

call Product\Build\buildenv.cmd

@echo on
rem Builds .NET code in build.cmd now
::dotnet build pdmsux.sln --configuration Release /m:1
::if %errorlevel% NEQ 0 goto :end

echo "Built .NET code"
rem Build frontend code
node -v
pushd Product\Source\ux
call node_modules\.bin\gulp.cmd vsts:build
popd
if %errorlevel% NEQ 0 goto :end
echo "Built Frontend Code"

:end
popd
exit /b %errorlevel%
