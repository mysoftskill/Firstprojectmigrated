function Get-IsCurrentUserAdministrator() {
    $prp = New-Object System.Security.Principal.WindowsPrincipal([System.Security.Principal.WindowsIdentity]::GetCurrent())
    return $prp.IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Test-RunningElevated() {
    if ((Get-IsCurrentUserAdministrator) -eq $false) {
        Write-Host "Please start the script elevated." -ForegroundColor Red
        exit
    }
}

function Set-WebSiteEndpointAcls([string] $DomainName, [string] $AclUser) {
    # Delete overly broad junk URL ACL entries that usually prevent our bindings from working.
    # For most dev boxes log entries will indicate failure to delete - it's OK. The key here is to get rid of these entires.
    netsh http delete urlacl url=https://+:443/ | Out-File "$tempFolder\SetWebSiteEndpointAcls.log" -Append
    netsh http delete urlacl url=https://$($DomainName):443/ | Out-File "$tempFolder\SetWebSiteEndpointAcls.log" -Append
    netsh http add urlacl url=https://$($DomainName):443/ user=$($AclUser) | Out-File "$tempFolder\SetWebSiteEndpointAcls.log" -Append
}

function Set-ProcessEnvVar($name, $value) {
    [Environment]::SetEnvironmentVariable($name, $value, "Process")
}

function Invoke-Process([string] $command, [string] $arguments, [switch] $doNotLogArguments) {
    $loggedArguments = $arguments
    if ($doNotLogArguments) {
        $loggedArguments = "<obfuscated>"
    }

    Write-Host "# Executing: $command $loggedArguments" -fore DarkGray

    if ($arguments) {
        $exitCode = (Start-Process -FilePath "$command" -ArgumentList "$arguments" -Wait -Passthru -WindowStyle Minimized).ExitCode
    }
    else {
        $exitCode = (Start-Process -FilePath "$command" -Wait -Passthru -WindowStyle Minimized).ExitCode
    }

    Write-Host "# Exit code: $exitCode" -fore DarkGray
    return $exitCode
}

function Install-Msi([string] $msi, [string] $logFileName, [string] $additionalParams) {
    $exitCode = Invoke-Process -Command "msiexec.exe" -Arguments "/qn /L*v $logFileName /i $msi $additionalParams"
    return $exitCode
}

function Uninstall-Msi([string] $productId, [string] $logFileName, [string] $additionalParams) {
    $exitCode = Invoke-Process -Command "msiexec.exe" -Arguments "/qn /L*v $logFileName /x $productId $additionalParams"
    return $exitCode
}

function Find-FirstFileInParent([string] $pattern, [string] $startDirectory = ".") {
    $match = ""
    $currentDirectory = Get-Item $startDirectory

    while (!$match -and $currentDirectory -ne $null) {
        $currentDirectory = Get-Item $currentDirectory.FullName
        $match = Get-ChildItem $currentDirectory -File -Filter $pattern | Select-Object -First 1

        $currentDirectory = $currentDirectory.Parent
    }

    return $match
}
