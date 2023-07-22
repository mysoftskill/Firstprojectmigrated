rem Init.cmd will run before the site is created in IIS. This is the place to do the following task if necessary: 
rem 	. Clean up to make sure web app starts in a clean condition, if it exits unexpectedly in the previous run.
rem 	. Prepare data for the site. e.g. Passport support's rpsserver.xml, etc.
rem 	. Install and configure third-party extension to IIS, e.g. URL Rewrite, Advanced Logging, etc

ECHO OFF
ECHO Running init.cmd %DATE% %TIME% - Environment: "%Environment%" - %~dp0

set DataDir=d:\data
set LogDir=%DataDir%\logs\DeployLog
set LogFile=%LogDir%\PdmsUx_init.log

if not exist %LogDir% mkdir %LogDir%

rem Install ASP.NET Core Module for IIS.
redist\dotnet-hosting-2.1.11-win.exe /install /quiet /log %LogDir%\ancm_install.log OPT_INSTALL_LTS_REDIST=0 OPT_INSTALL_FTS_REDIST=0 OPT_NO_X86=1
IF %ERRORLEVEL% NEQ 0 EXIT %ERRORLEVEL%

rem Install Rewrite for IIS.
redist\rewrite_amd64.msi /quiet /log %LogDir%\iisrewrite_install.log
IF %ERRORLEVEL% NEQ 0 EXIT %ERRORLEVEL%

GOTO STARTSERVICE

:STARTSERVICE

REM Per https://sharepoint/sites/autopilot/wiki/AutoIIS.aspx
REM IT IS IMPORTANT to exit with non-zero code if the script runs into error. This will notify AutoIIS that the script has failed, and AutoIIS will retry.
REM net start returns 0 when service starts successfully
IF %ERRORLEVEL% NEQ 0 EXIT %ERRORLEVEL%

powershell "& Set-ExecutionPolicy RemoteSigned"
powershell "& '.\init.ps1'"

:END
