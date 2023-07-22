Write-Host "Please select a file to be encrypted. If the file is encrypted correctly, the AP Manifest file (CSV) will also be regenerated." -ForegroundColor green

$SstoolPath = "$env:EnlistmentRoot\tools\autopilot\sstool"
$ApSignToolPath = "$env:EnlistmentRoot\tools\path1st"
$PxsPath = $env:pfxSrcRoot

if ($PxsPath -eq $null)
{
  Write-Host "Please make sure you are running in the PilotFish command environment." -ForegroundColor Red
  exit 1
}

try
{
  $OpenFileDialog = New-Object System.Windows.Forms.OpenFileDialog
}
catch
{
  Write-Host "Unable to initialize the system file picker. Please make sure you are running in the PilotFish command environment." -ForegroundColor Red
  exit 1
}
$OpenFileDialog.InitialDirectory = "$PxsPath\Data\Encrypted"
$OpenFileDialog.Title = "Pick a file to encrypt"
if ($OpenFileDialog.ShowDialog() -ne "OK")
{
  Write-Host "No file was selected. Please try again" -ForegroundColor Yellow
  exit 1
}
if (-Not (Test-Path $OpenFileDialog.FileName))
{
  Write-Host "$OpenFileDialog.FileName doesn't exist" -ForegroundColor Red
  exit 1
}


$Folder = (Get-Item $OpenFileDialog.FileName).DirectoryName
$VETopLevelFolder = "WDG_MEE"
if ($OpenFileDialog.FileName -match 'sandbox-pf')
  {
    $EnvironmentFolder = "PXS-Sandbox"
    $PfEnvironment = @{ 
      "sstool" = "$VETopLevelFolder\$EnvironmentFolder";
      "apsigntool" = "VE-ROOT/$VETopLevelFolder/$EnvironmentFolder"
    }
  }
if ($OpenFileDialog.FileName -match 'ppe-pf')
  {
    $EnvironmentFolder = "PXS-PPE"
    $PfEnvironment = @{ 
      "sstool" = "$VETopLevelFolder\$EnvironmentFolder";
      "apsigntool" = "VE-ROOT/$VETopLevelFolder/$EnvironmentFolder"
    }
  }
if ($OpenFileDialog.FileName -match 'prod-pf')
  {
    $EnvironmentFolder = "PXS-Prod"
    $PfEnvironment = @{ 
      "sstool" = "$VETopLevelFolder\$EnvironmentFolder";
      "apsigntool" = "VE-ROOT/$VETopLevelFolder/$EnvironmentFolder"
    }
  }

  # -e (encrypt)
  # -g (group, aka path to the VE/Folder for an AP environment)
  # -i (input file)
  # -o (output file)
  # will generate a command like this:
  #   sstool -e -g "WDG_MEE\PXS-PROD" -i E:\src\MEE.Privacy.Experience.Svc\Data\Encrypted\sandbox-pf\PrivacyExperienceServiceWD\Configurations\PrivacyUserConfiguration.ini -o E:\src\MEE.Privacy.Experience.Svc\Data\Encrypted\prod-pf\PrivacyExperienceServiceWD\Configurations\PrivacyUserConfiguration.ini.encr
  & "$SstoolPath\sstool" -e -g $PfEnvironment.sstool -i $FileName -o "$FileName.encr"
  
  if ($LASTEXITCODE -ne 0)
  {
    Write-Host "Unable to encrypt file $FileName" -ForegroundColor Red
    exit 1
  }

  # --signManifest (sign single folder)
  # -w (workflow)
  # -r (path for signature)
  # -i (AP environment folder)
  # will generate a command like this: 
  #   apsigntool --signManifest -w data -r E:\src\MEE.Privacy.Experience.Svc\Data\Encrypted\prod-pf\PrivacyExperienceServiceWD\Configurations -i VE-ROOT/WDG_MEE/PXS-Prod
& "$ApSignToolPath\apsigntool" --signManifest -w data -r $Folder -i $PfEnvironment.apsigntool

if ($LASTEXITCODE -ne 0)
{
  Write-Host "Unable to generate manfest" -ForegroundColor Red
  exit 1
}
exit