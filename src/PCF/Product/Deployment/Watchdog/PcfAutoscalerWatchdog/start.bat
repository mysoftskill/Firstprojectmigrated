REM lets party
powershell.exe .\ProcessStatusCheck.ps1 -processName Pcf.Autoscaler -minRunningSeconds 180
ECHO %errorlevel%
