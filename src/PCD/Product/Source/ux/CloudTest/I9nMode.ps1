param (
	[switch]$start = $false,
	[switch]$stop = $false,
	[string][ValidateSet("local", "local-dev", "vsts-build")]$context = "local",
	[switch]$cloudtest = $false,
	[string]$gulpLocation,
	[switch]$runTests = $false
)
# This script is called by npm commands to perform actions related to the protractor tests
Set-Variable maxRetry -option Constant -value 10
Set-Variable uxBaseLocationDebug -option Constant -value "bin\Debug"

# Basic input argument validations
if ($start -eq $stop) {
	Write-Host "Please specify either -start or -stop argument." -ForegroundColor "Red"
	return
}
if ($runTests -and !$start) {
	Write-Host "-runTests switch needs to be used in conjunction with -start." -ForegroundColor "Red"
	return
}
if ($context -eq "vsts-build" -and $runTests -and !$gulpLocation) {
	Write-Host "-gulpLocation needs to be specified if running tests from vsts build." -ForegroundColor "Red"
	return
}

if ($start) {

	# If ux.exe is already running in dev mode or local, don't start a new one.
	$uxProcess = Get-Process ux -ErrorAction SilentlyContinue
	if ($runTests -and $uxProcess) {
		if ($context -eq "local-dev") {
			gulp protractor:start:dev
		}
		if ($context -eq "local") {
			gulp protractor:start
		}
		return;
	}

	Write-Host "Setting env variable..."
	[Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development", "Process")

	# Resolve location of ux.exe based on context
	switch ($context) {
		"vsts-build" {
			# Working folder is set appropriately within build definition.
			$filePathArg = "ux.exe"
			Break
		}
		default {
			$filePathArg = "{0}\ux.exe" -f $uxBaseLocationDebug
			Break
		}
	}
	if($cloudtest){
		$filePathArg = "[WorkingDirectory]\intTest\ux.exe"
		Write-Host $filePathArg
		$filePathArg = "..\ux.exe"
		Write-Host $filePathArg
		# Start ux.exe in i9n mode
		$env:ASPNETCORE_ENVIRONMENT="Development"
		$env:ASPNETCORE_URLS="https://localhost:5000"
		dotnet dev-certs https --trust
		Write-Host "Launching ux.exe in i9n mode..."
		cd ..
		Start-Process -FilePath "ux.exe" -argument "--i9nMode"
		Get-Process -Id (Get-NetTCPConnection -LocalPort 5000).OwningProcess
		cd CloudTest
	}
	else{
		Write-Host "Launching ux.exe in i9n mode..."
		Start-Process -FilePath $filePathArg -argument "--i9nMode"
	}
	# Function that tests conection to a specific hostname:port at intervals of 1 sec, upto max 10 tries.
	function Test-Host-Port($hostname, $port, $try)
	{
		$int = [int]$try

		if ($try -ge $maxRetry) {
			# Max number of tries exceeded.
			Write-Host "Max number of tries exceeded, stopping connection test." -ForegroundColor "Red"
			return
		}

		$testResult = Test-NetConnection -ComputerName $hostname -Port $port
		if ($testResult.TcpTestSucceeded) {

			# We got successful connection.
			# Note: This step of opening the site is required on build agents as well. This is to 'initialize' the site
			# on Chrome application, so that the headless Chrome instance can access it without initialization delay. 
			Write-Host "Connection successful, opening site..."
			$urlArg = "--url https://{0}:{1}?mocks=true" -f $hostname, $port
			Start-Process -FilePath "chrome.exe" -ArgumentList $urlArg

			Write-Host "Warming up service..."
			$urlArg = "https://{0}:{1}" -f $hostname, $port
			Invoke-WebRequest -URI $urlArg | out-null

			if ($runTests) {
				# Resolve gulp location conditionally and start testing.
				switch ($context) {
					"local-dev" {
						Write-Host "Starting i9n testing..."
						gulp protractor:start:dev
						Break
					}
					"vsts-build" {
						Write-Host "Logging Chrome version..."
						(Get-Item (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe').'(Default)').VersionInfo

						Write-Host "Starting i9n testing..."
						pushd $gulpLocation
						node_modules\.bin\gulp.cmd protractor:start:vsts
						popd
						Break
					}
					default {
						Write-Host "Starting i9n testing..."
						gulp protractor:start
						Break
					}
				}
			}
		} else {
		
			# Still no connection, need to retry.
			Start-Sleep -Milliseconds 1500
			Write-Host "No connection, retrying..."

			$nextTry = $try + 1
			Test-Host-Port $hostname $port $nextTry
		}
	}

	Write-Host "Testing connection to localhost..."
	Test-Host-Port localhost 5000 1

} else {
	Write-Host "Stopping ux.exe in i9n mode..."
	$exists = Get-Process -name ux -ErrorAction SilentlyContinue
	if($exists){
		Stop-Process -name ux -Force
	}
	else{
		Write-Host "ux.exe was not running"
	}
}
