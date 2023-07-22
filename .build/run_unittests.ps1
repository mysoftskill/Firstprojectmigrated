param([string] $bin, [string] $unittestrunsettings)
$RUN_SETTINGS_FILE = if ([string]::IsNullOrWhitespace($unittestrunsettings)) { "PXSUnitTest.runsettings" } else { $unittestrunsettings }
Write-Host (Get-ChildItem -Path $bin -recurse -File *.UnitTests.*dll | ? { $_.FullName -notmatch "\\obj\\?" }).FullName
dotnet test (Get-ChildItem -Path $bin -recurse -File *.UnitTests.*dll | ? { $_.FullName -notmatch "\\obj\\?" }).FullName --logger:trx --settings:$RUN_SETTINGS_FILE