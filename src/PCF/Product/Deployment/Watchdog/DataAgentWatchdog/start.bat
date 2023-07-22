REM lets party
powershell.exe .\ProcessStatusCheck.ps1 -processName Pcf.DataAgent -minRunningSeconds 180
ECHO %errorlevel%
