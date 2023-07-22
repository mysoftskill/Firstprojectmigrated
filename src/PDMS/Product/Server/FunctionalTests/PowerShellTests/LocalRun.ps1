# Run this script with admin privilege from the build output directory

robocopy ..\..\..\Pdms.PowerShell .\PDMS /MIR
robocopy ..\..\..\Pdms.PowerShell.TestHook .\PDMSTestHook /MIR

.\PesterTest.ps1 .\testResult.xml
