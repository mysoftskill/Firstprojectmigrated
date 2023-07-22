@if "%_echo%" == "" (
    echo off
) else (
    echo Arguments: %*
)

goto :START

:HELP
    echo/
    echo PCF build command line helper.
    echo/
    echo  USAGE: build ^<command^>
    echo/
    echo  debug      Full debug build
    echo  release    Full release build
    echo  clean      clean the workspace
    echo  restore    .NET restore and clean.
    echo  postbuild  Assemble the AutopilotRelease folder
    echo/
    echo  Writes warnings and errors to build.log
    echo/

exit /b

:START
setlocal enableextensions enabledelayedexpansion

for /f "tokens=1*" %%i in ("%*") do set Arguments=!Arguments!%%j

if /i "%~1" == "debug" ( call :Debug
) else if /i "%~1" == "release" ( call :Release
) else if /i "%~1" == "clean" ( call :clean
) else if /i "%~1" == "postbuild" ( call :postbuild
) else if /i "%~1" == "restore" ( call :restore
) else if /i "%~1" == "-?" ( endlocal & call :HELP 
) else (
  endlocal & call :HELP
)
exit /b

:Debug
msbuild dirs.proj /p:Configuration=Debug /p:Platform=x64 /m /fl /flp:LogFile=build.log;WarningsOnly;ErrorsOnly
exit /b

:Release
msbuild dirs.proj /p:Configuration=Release /p:Platform=x64 /m /fl /flp:LogFile=build.log;WarningsOnly;ErrorsOnly
exit /b

:postbuild
msbuild deployment/postbuild.proj /p:Configuration=Release /p:Platform=x64 /t:Build /fl /flp:LogFile=build.log;WarningsOnly;ErrorsOnly
exit /b

:restore
dotnet restore --interactive
exit /b

:clean
cd ..
if exist ..\NugetPackages rmdir ..\NugetPackages /s /q
git clean -xdf
cd /

exit /b
