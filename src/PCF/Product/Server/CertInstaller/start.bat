pushd %~dp0
:: Temporary disable
set PCF_EnvironmentName

if "%PCF_EnvironmentName%"=="" (
    echo ERROR: PCF_EnvironmentName is not set.
    exit 1
)

if "%PCF_EnvironmentName%"=="OneBox" (
    echo Run src\PCF\Product\Tools\ProvisionDevMachine.ps1 to setup certs and rps in OneBox environment.
) else (
    CommonSetup.exe
)

popd
