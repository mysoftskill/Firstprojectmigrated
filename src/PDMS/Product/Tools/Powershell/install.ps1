param(
	[switch]
	$SAW
)

if ($SAW) {
	$dest ="$($Env:userprofile)\Work Folders\Documents\WindowsPowerShell\Modules"
}
else {	
	$dest ="$($Env:userprofile)\Documents\WindowsPowerShell\Modules"
}

robocopy "PDMS" "$dest\PDMS" /s /r:0 /w:0 /log:copy-pdms.txt

if ($LastExitCode -gt 3) {
	Write-Warning "Error copying files. Ensure all powershell windows are closed."
}

robocopy "PDMSGraph" "$dest\PDMSGraph" /s /r:0 /w:0 /log:copy-pdmsgraph.txt

if ($LastExitCode -gt 3) {
	Write-Warning "Error copying files. Ensure all powershell windows are closed."
}