REM lets party
powershell.exe .\ProcessStatusCheck.ps1 -processName PCF.QueueDepth -minRunningSeconds 180
ECHO %errorlevel%
