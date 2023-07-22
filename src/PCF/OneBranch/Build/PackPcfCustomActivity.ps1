param
(
    [Parameter(Mandatory=$true)]
    [string]$BuildOutput,
    [Parameter(Mandatory=$true)]
    [string]$ArchiveName
)

$compressParameters = @{
    Path = "$BuildOutput\*.*", "$BuildOutput\SampleADFArmTemplate\*.json"
    CompressionLevel = "Fastest"
    DestinationPath = $ArchiveName
}

Compress-Archive @compressParameters
