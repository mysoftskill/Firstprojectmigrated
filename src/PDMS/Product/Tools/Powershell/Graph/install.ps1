param(
	[switch]
	$SAW
)

if ($SAW) {
	$dest ="$($Env:userprofile)\Work Folders\Documents\WindowsPowerShell\Modules\PDMSGraph"
}
else {	
	$dest ="$($Env:userprofile)\Documents\WindowsPowerShell\Modules\PDMSGraph"
}
Write-Host $dest
Get-ChildItem -Path . -Recurse | Unblock-File
robocopy . $dest /s /r:0 /w:0 /log:copy.txt

if ($LastExitCode -gt 3) {
	Write-Warning "Error copying files. Ensure all powershell windows are closed."
}