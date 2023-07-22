param
(
[Parameter(Mandatory=$true)]
[string]$SourceDir,
[Parameter(Mandatory=$true)]
[string]$ArchiveName
)

Compress-Archive -Path $SourceDir $ArchiveName