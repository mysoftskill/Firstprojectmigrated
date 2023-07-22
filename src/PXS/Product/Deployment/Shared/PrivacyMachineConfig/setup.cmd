@ECHO OFF

IF "%~1"=="" (
  ECHO Incorrect Usage
  EXIT /B 1
)

SET NEXTISEVENT=""
SET EVENT=""
:AGAIN
IF NOT "%~1"=="" (
  IF %NEXTISEVENT%=="1" (
    SET EVENT=%~1
    GOTO DONE
  )
  IF /I "%~1"=="-event" (
    SET NEXTISEVENT="1"
  )
  SHIFT
  GOTO AGAIN
)
:DONE

ECHO Event: %EVENT%
IF /I NOT "%EVENT%"=="start" (
  ECHO Not Startup
  EXIT /B 0
)

ECHO Start .\ConfigureMachine.ps1
powershell -ExecutionPolicy RemoteSigned -File .\ConfigureMachine.ps1
ECHO End .\ConfigureMachine.ps1 (%ERRORLEVEL%)
