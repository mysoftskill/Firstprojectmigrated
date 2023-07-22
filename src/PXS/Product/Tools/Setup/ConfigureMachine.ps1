$ErrorActionPreference = "Stop"
[string]$tempFolder= [System.IO.Path]::Combine("c:\temp", (New-Guid).ToString('N'))

function MatchName([string] $name)
{
	return $name.ToLower().Replace(".", "").Replace("-", "").Replace("pfx", "").Replace("encr", "").Replace("password", "").Replace("new", "").Replace("txt", "").Replace("dat", "").Replace(" ", "")
}

function IsServiceFabric()
{
	if (Test-Path env:Fabric_ApplicationName)
	{
		Write-Host "This is a ServiceFabric application"
		return $true
	}

	Write-Host "This is a DevBox"
	return $false
}

function GrantNetworkServiceAccess([System.Security.Cryptography.X509Certificates.X509Certificate2] $cert)
{
	$rsaFile = $cert.PrivateKey.CspKeyContainerInfo.UniqueKeyContainerName
	$rsaPath = "$env:ALLUSERSPROFILE\Microsoft\Crypto\RSA\MachineKeys\$rsaFile"
	$acl = Get-Acl -Path $rsaPath
	$permission = "NETWORK SERVICE","Read","Allow"
	$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
	$acl.AddAccessRule($accessRule)
	Set-Acl $rsaPath $acl
	Write-Host NetworkService permission granted
}

function CheckRpsVersionInstalledInGAC([string] $version)
{
		$content = . .\GACUtil\gacutil.exe /l Microsoft.Passport.RPS
		$content = $content -join "`r`n"
		Write-Host $content
		if ($content.Contains("Microsoft.Passport.RPS, Version=$version"))
		{
			return $true
		}

		return $false
}

function InstallVcRedist2013
{
	Write-Host "Starting install of Visual C++ 2013 Redistributable Package (x64)"
	. .\vcredist_2013\vcredist_x64.exe /q /norestart
	Write-Host "Finished installing Visual C++ 2013 Redistributable Package (x64)"
}

function WaitForUninstall
{
	while ($true)
	{
		$taskList = tasklist /FI "IMAGENAME eq msiexec.exe" | findstr /i msiexec

		if($taskList -ne $null)
		{
			break;
		}
		else
		{
			Write-Host "Waiting for Uninstall..."
			Sleep 1
		}
	}

	Write-Host "Uninstaller done"
}

function CleanRPS
{
	Write-Host "Beginning cleanup of RPS from GAC"
	. .\GACUtil\gacutil.exe /u Microsoft.Passport.RPS
	. .\GACUtil\gacutil.exe /u Microsoft.Passport.RPS.Native

	Write-Host "Finished cleanup of RPS from GAC"

	Write-Host "Begin cleanup of RPS installation folder"
	rd -Recurse -Path "$env:ProgramFiles\Microsoft Passport RPS\" -ErrorAction Ignore
	rd -Recurse -Path "${env:ProgramFiles(x86)}\Microsoft Passport RPS\" -ErrorAction Ignore
	Write-Host "Finished cleanup of RPS installation folder"
}

function UninstallRPS([string] $version)
{
	$isInGAC = CheckRpsVersionInstalledInGAC $version
	if ($isInGAC -eq $false)
	{
		Write-Host "Old RPS Version $version is not in GAC. Skipping Uninstall."
		return
	}

	Write-Host "Old RPS Version $version is in GAC. Beginning Uninstall..."

	Write-Host "Stopping Passport RPS"
	net stop "Passport RPS Service"

	Write-Host Uninstalling previous RPS version ($version)
	# Guid value is the product id of RPS. Trying to install by using the msi instead does NOT uninstall RPS properly.
	# Product id can be found by using this command in a powershell window, and looking for 'Windows Live ID Server'. Note this command takes a while to run. 
	#  get-wmiobject Win32_Product | Format-Table IdentifyingNumber, Name, LocalPackage -AutoSize
	. msiexec /qn /uninstall "{CF4F890D-3F6C-47CF-B7FD-9573F4FE7978}" /l rps$version.msi.Uninstall.log

	# Not ideal to sleep, but the msi installer launches many uninstaller windows 
	# and continuing on while uninstall is happening could cause install to happen while uninstall is still executing
	$waitTimeSeconds = 30
	Write-Host Waiting for $waitTimeSeconds seconds for uninstall to finish
	Sleep $waitTimeSeconds
	WaitForUninstall

	CleanRPS

	while ($true)
	{
		$isInGAC = CheckRpsVersionInstalledInGAC $version
		if ($isInGAC -eq $true)
		{
			Write-Host "Uninstall failure! RPS was not removed from GAC"
			return 
		}
		else
		{
			break;
		}
	}

	Write-Host "Finished Uninstall of RPS Version $version"
}

function InstallRPS([string] $path, [string] $version)
{
	Write-Host "Installing RPS from path: $path"
	Write-Host "Expected version to find in GAC is: $version"
	. msiexec /qn /i $path /l rps$version.msi.Install.log ALLUSERS=1
	while ($true)
	{
		$isInGAC = CheckRpsVersionInstalledInGAC $version

		if ($isInGAC -eq $true)
		{
			break;
		}

		Write-Host "Waiting for install..."
		Sleep 1
	}
	Write-Host Installed RPS.

	Write-Host Setting RPS Start/Stop Permissions for NETWORK SERVICE
	# https://blogs.msmvps.com/erikr/2007/09/26/set-permissions-on-a-specific-service-windows/
	# We need the (A;;RPWPDT;;;NS) added (A=Allow;;RP=ServiceStart,WP=ServiceStop,DT=ServicePauseContinue;;;NS=NETWORKSERVICE)
	# There is only replace, so the rest was grabbed from a regular AP machine.
	& sc.exe sdset RPSSvc "D:(A;;CCLCSWRPWPDTLOCRRC;;;SY)(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;BA)(A;;CCLCSWLOCRRC;;;IU)(A;;CCLCSWLOCRRC;;;SU)(A;;RPWPDT;;;NS)S:(AU;FA;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;WD)"
	Write-Host Permissions set

	Write-Host Setting permissions on config dir for NETWORK SERVICE
	$rpsPath = "$env:ProgramFiles\Microsoft Passport RPS\config"
	$acl = Get-Acl -Path $rpsPath
	$permission = "NETWORK SERVICE","FullControl","ContainerInherit,ObjectInherit","None","Allow"
	$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
	$acl.AddAccessRule($accessRule)
	Set-Acl $rpsPath $acl
	Write-Host NetworkService permission granted
}

function SetupNetworkServiceAcl
{
	# Setup HTTPS for NetworkService
	& netsh http delete urlacl url=https://+:443/
	& netsh http add urlacl url=https://+:443/ user="NETWORK SERVICE" listen=yes
}

function PrepareLockFile
{
	$lockFolder = [System.IO.Path]::GetFullPath("$env:DATADIR\PrivacyMachineConfig\")
	[string] $lockFileName = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($lockFolder, "lock.txt"))

	if(!(Test-Path $lockFolder))
	{
		Write-Host "Folder '$lockFolder' does not exist, so creating for the first time"
		New-Item -Path $lockFolder -ItemType "directory" -Force | Out-Null
	}

	return $lockFileName
}

function AcquireLock([string] $lockFileName)
{
	$stopwatch =  [System.Diagnostics.Stopwatch]::StartNew()
	$maxWaitSeconds = 300
	$lockSleepTimerSeconds = 1
	do 
	{
		$lockstatus = $false
		
		$status = $false
		try 
		{
			#Lock File
			Write-Host "Attemping to acquire file lock on file name: $lockFileName"
			$lockFile = [System.IO.File]::Open($lockFileName,'OpenOrCreate','ReadWrite','None')
			$status = $true
		} 
		catch 
		{
			Write-Warning $_
			$status = $false
		}
		
		if ($status -eq $true)
		{
			# open handle == lock was successful
			if ($lockFile.Handle -ne $null)
			{
				Write-Host "Lock acquired"
				$lockstatus = $true
			}
		} 
		else
		{
			Write-Host "Cannot acquire lock. Sleeping for $lockSleepTimerSeconds seconds"
			Sleep $lockSleepTimerSeconds

			if ($stopwatch.Elapsed.TotalSeconds -gt $maxWaitSeconds)
			{
				Write-Host "Failed to acquire lock in time: $maxWaitSeconds seconds."
				if ($lockFile -ne $null)
				{
					$lockFile.Close()
					$lockFile = $null
				}

				return $null
			}
		}

		Write-Debug "LockStatus = $lockstatus"
	}
	until (($lockstatus -eq $true) -or ($stopwatch.Elapsed.TotalSeconds -gt $maxWaitSeconds))

	return $lockFile
}

Set-Location -Path $PSScriptRoot
[Environment]::CurrentDirectory = $PSScriptRoot

$currentPrincipal = New-Object Security.Principal.WindowsPrincipal( [Security.Principal.WindowsIdentity]::GetCurrent( ) )
if ( -not ($currentPrincipal.IsInRole( [Security.Principal.WindowsBuiltInRole]::Administrator ) ) )
{
    Write-Error "This script must be executed in admin mode." -ErrorAction Stop
}
else 
{     
    Write-Host "You are running in admin mode!"
}

# RPS msi location differs from local machine and build. 
# The reason for this is referencing the msi from a NuGet package location or the build drop. This helps avoid checking in msi's into our git repo.
if (IsServiceFabric)
{
	[string]$RpsMsiFolder = [System.IO.Path]::Combine($PSScriptRoot, "RPS\7.1.6819")
	Write-Host "RpsMsiFolder: $RpsMsiFolder"
	[string]$RpsMsiFileName = "rps64.msi"
}
else 
{
	[string]$RpsMsiFolder = [System.IO.Path]::Combine($HOME, ".nuget\packages\rps.official.amd64.msa\7.1.6819\msi")
	Write-Host "RpsMsiFolder: $RpsMsiFolder"
	[string]$RpsMsiFileName = "rps64.msi"
}

[System.IO.FileStream]$lockFile = $null
[string]$lockFileName = PrepareLockFile
[string]$rpsMsiFullPath = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($RpsMsiFolder, $RpsMsiFileName))

try
{
	$lockFile = AcquireLock $lockFileName

	if ($lockFile -ne $null)
	{
		InstallVcRedist2013

		UninstallRPS "6.7.6643.0"
		
		if (-Not [System.IO.File]::Exists($rpsMsiFullPath))
		{
			Write-Host "The rps msi installer did not exist at the path: $rpsMsiFullPath"
			exit 1
		}

		Write-Host "The rps msi installer exists at this path:  $rpsMsiFullPath. Continuing to start install..."

		InstallRPS $rpsMsiFullPath "7.1.0.0"
		SetupNetworkServiceAcl
	}
	else
	{
		Write-Host "Failed to acquire lock."
		exit 1
	}
}
finally
{
	if ($lockFile -ne $null)
	{
		Write-Host "Releasing configure machine lock"
		$lockFile.Close()
		Remove-Item $lockFileName
	}
}

# Note, this script does not run SSL thumbprint binding to port 443 currently. This functionality has been moved to the AzureKeyVaultCertificateInstaller.
# However, if refactoring is done to have the AzureKeyVaultCertificateInstaller return the thumbprint of the SSL certificate, this can be modified to do binding here instead (see script change history).
