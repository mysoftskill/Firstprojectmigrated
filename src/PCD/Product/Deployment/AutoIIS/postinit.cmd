rem PostInit.cmd will run after the site is created in IIS. This is the place to do the following task if necessary: 
rem 	. Customize IIS application pool settings or application settings using appcmd. 
rem 	. Warm-up query
ECHO OFF

ECHO Set custom IIS logging fields
:Config and field definitions https://www.iis.net/configreference/system.applicationhost/sites/sitedefaults/logfile
rem %windir%\System32\inetsrv\appcmd.exe set config -section:sites -siteDefaults.logFile.logExtFileFlags:Date,Time,ClientIP,UserName,ServerIP,Method,UriStem,HttpStatus,Win32Status,BytesSent,BytesRecv,TimeTaken,UserAgent,HttpSubStatus

:config IIS app pool recycle settings
:https://weblogs.asp.net/owscott/why-is-the-iis-default-app-pool-recycle-set-to-1740-minutes
rem %windir%\System32\inetsrv\appcmd.exe set AppPool /apppool.name:"AUTOIISPOOL_MeePortal" /recycling.periodicRestart.time:29:00:00