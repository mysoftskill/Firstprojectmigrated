# ------------------------------------------------------------
#  Copyright (c) Microsoft Corporation. All rights reserved.
#  Licensed under the MIT License (MIT).
# ------------------------------------------------------------

# This script is directly copied from this ADO task: https://marketplace.visualstudio.com/items?itemName=ms-openapi.OpenApiDocumentTools
# DO NOT MODIFY

<#
    Invoke-CSharpAnnotationsDocumentGeneration.ps1

    .SYNOPSIS
    A harness for invoking the Microsoft OpenAPI.NET C# Annotations Document Generation library to produce OpenAPI documents.

    .DESCRIPTION
    This harness is responsible for producing OpenAPI document(s) based on C# assemblies and a Visual Studio annotation
    XML given as input. The resulting documents will be written to the desired output location. When the OpenAPI spec
    version to produce is omitted, the latest version will be produced.

    .EXIT CODES
    0 = Produced an OpenAPI document with no errors.
    1 = Produced an OpenAPI document with one or more errors.

    .OUTPUT FILES
    All files are written in UTF-8 encoding without a byte order mark (BOM).

    1. An OpenAPI document per variant.

       File names:
         - OpenApiDocument.{openApiSpecVersion}.{format}
         - OpenApiDocument.{openApiSpecVersion}.{title}.{format}

    2. A metadata document per variant.

       File names:
         - OpenApiDocument.{openApiSpecVersion}.VariantInfo.json
         - OpenApiDocument.{openApiSpecVersion}.{title}.VariantInfo.json

    3. Document containing OpenAPI document generation errors.

       File name: OpenApiDocument.{openApiSpecVersion}.BuildErrors.txt
       Note, this document will not exist unless there is at least one error as a result of generation.
#>

[CmdletBinding()]
param(
    # Path to the directory containing assembly dependencies (e.g. Microsoft.OpenApi.dll, etc).
    [Parameter(Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [string]
    $DependentAssembliesDirectoryPath,

    # Path to the directory which will contain the resulting OpenAPI document(s).
    [Parameter(Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [string]
    $OutputPath,

    # Version of the document to produce.
    [Parameter(Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [string]
    $DocumentVersion,

    # Paths of the Visual Studio XML documenation files containing descriptions for the service endpoints and contracts.
    [Parameter(Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [string[]]
    $VisualStudioXmlDocumentationPaths,
    
    # Paths of the assembly files which implement service endpoints and contracts.
    [Parameter(Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [string[]]
    $AssemblyPaths,

    # Major version of the OpenAPI spec to produce document for.
    [ValidateRange(0, [int]::MaxValue)]
    [int]
    $MajorOpenApiSpecificationVersion,

    # Minor version of the OpenAPI spec to produce document for.
    [ValidateRange(0, [int]::MaxValue)]
    [int]
    $MinorOpenApiSpecificationVersion,
    
    # Path of the optional C# Annotations Document Generation advanced configuration XML file.
    [string]
    $AdvancedConfigurationXmlDocumentPath,

    # Format of the OpenAPI document(s) to produce.
    [ValidateSet("JSON", "YAML")]
    [string]
    $Format = "JSON",
    
    # Value indicating whether to generate OpenAPI document where Schema properties are formatted with camel case.
    [switch]
    $UseCamelCaseForSchemaProperties = $false,

    # Paths of the assembly files which hold the additional filters.
    [string[]]
    $FilterPaths,
    
    # Class names of the additional filters to add. 
    [string[]]
    $FilterClassNames,
    
    # Path of the optional file containing the OpenAPI document description.
    [string]
    $DocumentDescriptionFilePath
)

#----------------------------------------------------------------------------------------------------------------------
# Define global constants.
#----------------------------------------------------------------------------------------------------------------------
$ErrorExitCode = 1

#----------------------------------------------------------------------------------------------------------------------
# Define functions.
#----------------------------------------------------------------------------------------------------------------------
function Aggregate-GenerationDiagnosticErrors {
    <#
        .DESCRIPTION
        Aggregates document and operation errors sourced from a GenerationDiagnostic into a single array.

        .RETURNS
        An array of document and operation errors.
    #>
    [CmdletBinding()]
    param (
        # Generation diagnostic containing errors to aggregate.
        [Parameter(Mandatory=$true)]
        [ValidateNotNull()]
        [Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Models.GenerationDiagnostic]
        $GenerationDiagnostic
    )
    
    $aggregateErrors = New-Object System.Collections.Generic.List[string]

    if ($GenerationDiagnostic.DocumentGenerationDiagnostic -and $GenerationDiagnostic.DocumentGenerationDiagnostic.Errors.Count -gt 0) {
        foreach ($generationError in $GenerationDiagnostic.DocumentGenerationDiagnostic.Errors) {
            if ($generationError) {
                $aggregateErrors.Add($(Serialize-GenerationError $generationError))
            }
        }
    }

    if ($GenerationDiagnostic.OperationGenerationDiagnostics) {
        foreach ($operationGenerationDiagnostic in $GenerationDiagnostic.OperationGenerationDiagnostics) {
            if ($operationGenerationDiagnostic -and $operationGenerationDiagnostic.Errors.Count -gt 0) {
                $operationId = "$($operationGenerationDiagnostic.OperationMethod.ToUpper()) $($operationGenerationDiagnostic.Path)"
                
                foreach ($generationError in $operationGenerationDiagnostic.Errors) {
                    if ($generationError) {
                        $aggregateErrors.Add("$operationId || $(Serialize-GenerationError $generationError)")
                    }
                }
            }
        }
    }

    return $aggregateErrors
}

function Create-DocumentGenerationSettings {
    <#
        .DESCRIPTION
        Creates a C# Annotations document generation settings object.

        .RETURNS
        Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.OpenApiDocumentGenerationSettings object based on inputs.
    #>
    [CmdletBinding()]
    param (
        # Value indicating whether to generate OpenAPI document where Schema properties are formatted with camel case.
        [switch]
        $UseCamelCaseForSchemaProperties
    )

    $schemaPropertyNameResolver = $null;

    if ($UseCamelCaseForSchemaProperties.IsPresent) {
        $schemaPropertyNameResolver = New-Object Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.CamelCasePropertyNameResolver
    }
    else {
        $schemaPropertyNameResolver = New-Object Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.DefaultPropertyNameResolver
    }
    
    $schemaGenerationSettings = New-Object `
        Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.SchemaGenerationSettings $schemaPropertyNameResolver

    return New-Object `
        Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.OpenApiDocumentGenerationSettings `
        -ArgumentList $schemaGenerationSettings
}

function Generate-OpenApiDocuments {
    <#
        .DESCRIPTION
        Loads and invokes the C# Annotations Document Generation library in order to produce OpenAPI document(s).

        .RETURNS
        An array with two elements,
          1. Dictionary of [Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Models.DocumentVariantInfo] to [Microsoft.OpenApi.Models.OpenApiDocument]
          2. Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Models.GenerationDiagnostic

        .EXCEPTIONS
        Throws System.TypeLoadException when the filter type cannot be found.
    #>
    [CmdletBinding()]
    param (
        # Version of the OpenAPI document(s) to produce.
        [Parameter(Mandatory=$true)]
        [string]
        $DocumentVersion,

        # Visual Studio XMLs containing descriptions for the service endpoints and contracts.
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [System.Xml.Linq.XDocument[]]
        $VisualStudioXmlDocuments,

        # Paths to the assembly files which implement service endpoints and contracts.
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $AssemblyPaths,

        # Optional C# Annotations Document Generation advanced configuration XML.
        [System.Xml.Linq.XDocument]
        $AdvancedConfigurationXmlDocument,

        # Optional C# Annotations document generation settings.
        [Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.OpenApiDocumentGenerationSettings]
        $DocumentGenerationSettings,

        # Optional description of the OpenAPI document(s) to produce.
        [string]
        $DocumentDescription,

        # Optional paths of filter assembly files.
        [string[]]
        $FilterPaths,
    
        # Optional class names of filters to apply at document generation time.
        [string[]]
        $FilterClassNames
    )

    # Create C# Annotations Document Generation configuration.
    #
    $filterSetVersion = [Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.FilterSetVersion]::V1
    $openApiGeneratorConfiguration = New-Object `
        Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.OpenApiGeneratorConfig `
        -ArgumentList $VisualStudioXmlDocuments,$AssemblyPaths,$DocumentVersion,$filterSetVersion

    $openApiGeneratorConfiguration.AdvancedConfigurationXmlDocument = $AdvancedConfigurationXmlDocument

    # Add an assembly resolver when custom filters are used, so older filter versions may be supported if there are no
    # breaking changes.
    #
    if ($FilterClassNames.Length -gt 0) {
        $onAssemblyResolve = [System.ResolveEventHandler] {
            param($sender, $e)
            $name = New-Object System.Reflection.AssemblyName -ArgumentList $e.Name

            foreach($a in [System.AppDomain]::CurrentDomain.GetAssemblies()) {
                if ($a.GetName().Name -eq $name.Name) {
                    # Warn when fullnames are different.
                    #
                    if ($a.FullName -ne $e.Name) {
                        Write-Host "Issue encountered when loading filter dependency $($e.Name)$filterClassName, it does not match VSTS dependency $($a.FullName). "`
                        "Loading the VSTS dependency for the filter to use. Consider upgrading filter dependency to newer version to ensure compatability."
                    }

                    if ($name.Name -eq "SharpYaml") { return $a }
                    if ($name.Name -eq "Microsoft.OpenApi") { return $a }
                    if ($name.Name -eq "Microsoft.OpenApi.Readers") { return $a }
                    if ($name.Name -eq "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration") { return $a }
                }

                if ($a.FullName -eq $e.Name) {
                    return $a
                }
            }

            return $null
        }

        [System.AppDomain]::CurrentDomain.add_AssemblyResolve($onAssemblyResolve)
    }

    # Set optional filters.
    #
    for ($i = 0; $i -lt $FilterClassNames.Length; $i++) {
        $filterPath = $FilterPaths[$i]
        $filterClassName = $FilterClassNames[$i]

        $filterAssembly = [System.Reflection.Assembly]::LoadFrom($filterPath)
        $filterType = $filterAssembly.GetType($filterClassName)

        # Checks for the case where multiple versions of dependent packages are present, but are not caught by assembly resolver.
        #
        if ($null -eq $filterType) {
            try {
                $assembly.GetTypes()
            } catch [System.Reflection.ReflectionTypeLoadException] {
                Write-Host "Issue encountered when loading filter $filterClassName from assembly $filterPath. `
                The assembly, or one of its dependencies, conflicts with a previously loaded dependency this VSTS task requires."
                
                # Identify assemblies with the same name, but different versions.
                #
                foreach($domainAssembly in [appdomain]::currentdomain.getassemblies()) {
                    foreach($referencedAssemblyName in $assembly.GetReferencedAssemblies()) {
                        if ([System.Reflection.AssemblyName]::ReferenceMatchesDefinition($referencedAssemblyName, $domainAssembly.GetName())) {
                            if ($domainAssembly.GetName().Version.CompareTo($referencedAssemblyName.Version) -ne 0) {
                                Write-Host "Filter dependency: $($domainAssembly.fullname)"
                                Write-Host "Task dependency: $($referencedAssemblyName.fullname)"
                            }
                        }
                    }
                }

                throw [System.TypeLoadException] "Failed to load filter $filterClassName."
            }
            catch [System.Exception] {
                throw [System.TypeLoadException] "Failed to load filter class with name '$filterClassName'."
            }
        }

        $filter = [System.Activator]::CreateInstance([Type]$filterType)
        $openApiGeneratorConfiguration.OpenApiGeneratorFilterConfig.Filters.Add($filter)
    }

    # Invoke generation via C# Annotations Document Generation API.
    #
    $originalAppBasePath = [System.AppDomain]::CurrentDomain.GetData("APPBASE")

    try {
        # Set the application base path to the location of our dependencies before invoking the C# Annotations Document Generation.
        # This is important because the library will attempt to create a new AppDomain for isolating dependencies of
        # its generation process. As part of creating this isolated domain, the C# Annotations Document Generation assembly will be
        # loaded; thus, if the application base path is not set to the location of C# Annotations Document Generation's assembly, it
        # will result in a not found exception.
        #
        [System.AppDomain]::CurrentDomain.SetData("APPBASE", $DependentAssembliesDirectoryPath)

        $openApiGenerator = New-Object Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.OpenApiGenerator
        $generationDiagnostic = $null

        $variantInfoToDocumentMap = $openApiGenerator.GenerateDocuments(
            $openApiGeneratorConfiguration,
            [ref] $generationDiagnostic,
            $DocumentGenerationSettings)

        # Set the description of all variants when applicable.
        #
        if ($DocumentDescription) {
            foreach ($keyValuePair in $variantInfoToDocumentMap.GetEnumerator()) {
                $keyValuePair.Value.Info.Description = $DocumentDescription;
            }
        }

        return @($variantInfoToDocumentMap, $generationDiagnostic)
    } finally {
        # Reset the application base path to its original location now that we are done invoking the library.
        #
        [System.AppDomain]::CurrentDomain.SetData("APPBASE", $originalAppBasePath)
    }
}

function Get-AssemblyFullName {
    <#
        .DESCRIPTION
        Gets the fully qualified name of an assembly.

        .RETURNS
        Fully qualified name of assembly.

        .EXCEPTIONS
        Throws FileNotFoundException when path does not exist.
    #>
    [CmdletBinding()]
    param (
        # Path to the XML file to load.
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Path
    )

    if (-not (Test-Path $Path)) {
        throw [System.IO.FileNotFoundException] "$Path not found."
    }

    return [System.Reflection.AssemblyName]::GetAssemblyName($Path).FullName
}

function Get-Enum {
    <#
        .DESCRIPTION
        Converts the given string value into an enum value of the specified type.

        .RETURNS
        Enum value.

        .EXCEPTIONS
        Throws FileNotFoundException when assembly path is defined, but does not exist.
    #>
    [CmdletBinding()]
    param (
        # Type of enum to convert string value into.
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $EnumType,

        # String value to convert.
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Value,

        # Optional path to assembly encapsulating enum type. This should be defined when getting an enum from a custom
        # assembly as opposed to mscorlib.
        # See https://stackoverflow.com/questions/18912640/cannot-system-typegettype-of-type-from-assembly-system-configuration
        [string]
        $AssemblyPath
    )

    $type = $null

    if ($AssemblyPath) {
        if (-not (Test-Path $AssemblyPath)) {
            throw [System.IO.FileNotFoundException] "$AssemblyPath not found."
        }

        $type = [System.Type]::GetType("$EnumType, $(Get-AssemblyFullName $AssemblyPath)")
    }
    else {
        $type = [System.Type]::GetType($EnumType)
    }
    
    try {
        return [Enum]::Parse($type, $Value, $true)
    }
    catch {
        throw [System.ArgumentException]::new(
            "The value '$Value' could not be parsed as an enum of type '$EnumType' while ignoring case.",
            $PSItem.Exception)
    }
}

function Get-OpenApiSpecVersion {
    <#
        .DESCRIPTION
        Converts the given major-minor versions into an Microsoft.OpenApi.OpenApiSpecVersion understood by C# Annotations
        Document Generation library. Defaults to latest OpenApiSpecVersion when none is specified.

        .RETURNS
        Microsoft.OpenApi.OpenApiSpecVersion value.
    #>
    [CmdletBinding()]
    param (
        # Major version of the OpenAPI spec to produce document for.
        [int]
        $MajorOpenApiSpecificationVersion,

        # Minor version of the OpenAPI spec to produce document for.
        [int]
        $MinorOpenApiSpecificationVersion
    )

    # When a version is not specified, default to getting the latest. Otherwise, get the one asked for.
    #
    if ($MajorOpenApiSpecificationVersion -eq 0) {
        $openApiSpecVersionValues = [Enum]::GetValues([System.Type]::GetType([Microsoft.OpenApi.OpenApiSpecVersion]))

        return Get-Enum `
            "Microsoft.OpenApi.OpenApiSpecVersion" `
            $openApiSpecVersionValues[$openApiSpecVersionValues.Length - 1] `
            $microsoftOpenApiAssemblyPath
    }
    else {
        return Get-Enum `
            "Microsoft.OpenApi.OpenApiSpecVersion" `
            "OpenApi$MajorOpenApiSpecificationVersion`_$MinorOpenApiSpecificationVersion" `
            $microsoftOpenApiAssemblyPath
    }
}

function Load-AssemblyByName {
    <#
        .DESCRIPTION
        Loads an assembly into the current app domain.
    #>
    [CmdletBinding()]
    param (
        # Name of the assembly.
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Name
    )

    Write-Host "Invoking: Add-Type -AssemblyName $Name"

    try {
        Add-Type -AssemblyName $Name
    } catch {
        Write-Error $_.Exception.LoaderExceptions[0].ToString()
    }
}

function Load-AssemblyByPath {
    <#
        .DESCRIPTION
        Loads an assembly into the current app domain.
    #>
    [CmdletBinding()]
    param (
        # Path of the assembly.
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Path
    )

    Write-Host "Invoking: Add-Type -Path $Path"

    try {
        Add-Type -Path $Path
    } catch {
        Write-Error $_.Exception.LoaderExceptions[0].ToString()
    }
}

function Load-XmlDocument {
    <#
        .DESCRIPTION
        Loads an XML file at path specified.

        .RETURNS
        XDocument as a result of loading XML file into memory.

        .EXCEPTIONS
        Throws FileNotFoundException when path does not exist.
    #>
    [CmdletBinding()]
    param (
        # Path to the XML file to load.
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Path
    )

    if (-not (Test-Path $Path)) {
        throw [System.IO.FileNotFoundException] "$Path not found."
    }

    return [System.Xml.Linq.XDocument]::Load($Path)
}

function Read-FileContent {
    <#
        .DESCRIPTION
        Reads the contents of a file as a string.

        .RETURNS
        String as a result of loading the file content into memory.

        .EXCEPTIONS
        Throws FileNotFoundException when path does not exist.
    #>
    [CmdletBinding()]
    param (
        # Path to the XML file to load.
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Path
    )

    if (-not (Test-Path $Path)) {
        throw [System.IO.FileNotFoundException] "$Path not found."
    }

    return [System.IO.File]::ReadAllText($Path)    
}

function Remove-InvalidFileNameCharacters {
    <#
        .DESCRIPTION
        Removes invalid file name characters from a string and returns the result.

        .RETURNS
        Value with illegal characters removed.
    #>
    [CmdletBinding()]
    param(
        [String]
        $Value
    )

    if (!$Value) {
        return $null
    }

    $invalidCharacters = [IO.Path]::GetInvalidFileNameChars() -join ''
    $replacement = "[{0}]" -f [RegEx]::Escape($invalidCharacters)
    
    return $Value -replace $replacement
}

function Serialize-GenerationError {
    <#
        .DESCRIPTION
        Serializes a Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Models.GenerationError

        .RETURNS
        The string representation of a Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Models.GenerationError.
    #>
    [CmdletBinding()]
    param (
        # Generation error to serialize.
        [Parameter(Mandatory=$true)]
        [ValidateNotNull()]
        [Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Models.GenerationError]
        $GenerationError
    )

    return "ExceptionType: $(Serialize-String $GenerationError.ExceptionType) || Message: $(Serialize-String $GenerationError.Message)"
}

function Serialize-String {
    <#
        .DESCRIPTION
        Serializes a string by handling null case.

        .RETURNS
        The string or "<null>" to indicate string was null.
    #>
    [CmdletBinding()]
    param (
        # String to serialize.
        [string]
        $Value
    )

    if (!$Value) {
        return "<null>"
    }

    return $Value
}

function Write-Errors {
    <#
        .DESCRIPTION
        Writes an array of errors to the host and a file.
    #>
    [CmdletBinding()]
    param (
        # Path to directory where output file will be written.
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $OutputPath,
        
        # Errors to write.
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $Errors,

        # Major version of the OpenAPI spec the errors pertain to.
        [int]
        $MajorOpenApiSpecificationVersion,

        # Minor version of the OpenAPI spec the errors pertain to.
        [int]
        $MinorOpenApiSpecificationVersion
    )

    Write-ErrorsToHost `
        $Errors `
        $MajorOpenApiSpecificationVersion `
        $MinorOpenApiSpecificationVersion

    Write-ErrorsToFile `
        $OutputPath `
        $Errors `
        $(Get-OpenApiSpecVersion $MajorOpenApiSpecificationVersion $MinorOpenApiSpecificationVersion)
}

function Write-ErrorsToHost {
    <#
        .DESCRIPTION
        Writes an array of errors to the host.
    #>
    [CmdletBinding()]
    param (
        # Errors to write.
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $Errors,

        # Major version of the OpenAPI spec the errors pertain to.
        [int]
        $MajorOpenApiSpecificationVersion,

        # Minor version of the OpenAPI spec the errors pertain to.
        [int]
        $MinorOpenApiSpecificationVersion
    )

    $openApiSpecVersion = "$MajorOpenApiSpecificationVersion.$MinorOpenApiSpecificationVersion"

    Write-Host "The following issues were encountered while building OpenAPI $openApiSpecVersion document(s):"

    foreach ($error in $Errors) {
        Write-Host "    $error"
    }
}

function Write-ErrorsToFile {
    <#
        .DESCRIPTION
        Writes an array of errors to a file.
    #>
    [CmdletBinding()]
    param (
        # Path to directory where output file will be written.
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $OutputPath,
        
        # Errors to write.
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $Errors,

        # Specification version of the OpenAPI document with errors.
        [Parameter(Mandatory=$true)]
        [Microsoft.OpenApi.OpenApiSpecVersion]
        $OpenApiSpecificationVersion
    )

    # Ready output file name parts.
    #
    $openApiSpecVersion = Remove-InvalidFileNameCharacters $OpenApiSpecificationVersion.ToString()
    
    # Construct output path.
    #
    $path = Join-Path $OutputPath "OpenApiDocument.$openApiSpecVersion.BuildErrors.txt"
    
    # Write error file to output destination in UTF-8 encoding and without BOM (byte order mark). 
    #
    [System.IO.File]::WriteAllLines($path, $Errors)
    
    # Log to host
    #
    Write-Host "Wrote file '$path'"
}

function Write-OpenApiDocument {
    <#
        .DESCRIPTION
        Writes an OpenAPI Document and its variant metadata to the specified location.
    #>
    [CmdletBinding()]
    param (
        # Path to the directory to write to.
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $OutputPath,
        
        # Path to the directory to write to.
        [Parameter(Mandatory=$true)]
        [Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.Models.DocumentVariantInfo]
        $DocumentVariantInfo,
        
        # OpenAPI document to write.
        [Parameter(Mandatory=$true)]
        [Microsoft.OpenApi.Models.OpenApiDocument]
        $OpenApiDocument,

        # Specification version of the OpenAPI document.
        [Parameter(Mandatory=$true)]
        [Microsoft.OpenApi.OpenApiSpecVersion]
        $OpenApiSpecificationVersion,
        
        # Format OpenAPI document serialized as a string.
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Format
    )

    # Ready output file name parts.
    #
    $openApiSpecVersion = Remove-InvalidFileNameCharacters $OpenApiSpecificationVersion.ToString()
    $title = Remove-InvalidFileNameCharacters $DocumentVariantInfo.Title
    $formatExtension = Remove-InvalidFileNameCharacters $Format.ToLower()
    
    # Construct output paths.
    #
    $openApiDocumentFileNameParts = New-Object System.Collections.Generic.List[string]    
    $openApiDocumentFileNameParts.Add("OpenApiDocument")
    $openApiDocumentFileNameParts.Add($openApiSpecVersion)
    if ($title) { $openApiDocumentFileNameParts.Add($title) }
    $openApiDocumentFileNameParts.Add($formatExtension)

    $variantInfoFileNameParts = New-Object System.Collections.Generic.List[string]    
    $variantInfoFileNameParts.Add("OpenApiDocument")
    $variantInfoFileNameParts.Add($openApiSpecVersion)
    if ($title) { $variantInfoFileNameParts.Add($title) }
    $variantInfoFileNameParts.Add("VariantInfo.json")

    $openApiDocumentPath = Join-Path $OutputPath $([String]::Join('.', $openApiDocumentFileNameParts))
    $variantInfoPath = Join-Path $OutputPath $([String]::Join('.', $variantInfoFileNameParts))
    
    # Serialize output file contents.
    #
    $serializedOpenApiDocument = [Microsoft.OpenApi.Extensions.OpenApiSerializableExtensions]::Serialize(
        $OpenApiDocument,
        $OpenApiSpecificationVersion,
        $(Get-Enum "Microsoft.OpenApi.OpenApiFormat" $Format $microsoftOpenApiAssemblyPath))
    $serializedVariantInfo =  If ($title) { '{ "Title": "' + $title + '" }' } Else { '{ "Title": null }' }

    # Write files to their respective destinations in UTF-8 encoding and without BOM (byte order mark).
    #
    [System.IO.File]::WriteAllText($openApiDocumentPath, $serializedOpenApiDocument)
    [System.IO.File]::WriteAllText($variantInfoPath, $serializedVariantInfo)

    # Log to host.
    #
    Write-Host "Wrote file '$openApiDocumentPath'"
    Write-Host "Wrote file '$variantInfoPath'"
}

#----------------------------------------------------------------------------------------------------------------------
# Load dependent assemblies.
#----------------------------------------------------------------------------------------------------------------------
$sharpYamlAssemblyPath = Join-Path $DependentAssembliesDirectoryPath "SharpYaml.dll"
$microsoftOpenApiAssemblyPath = Join-Path $DependentAssembliesDirectoryPath "Microsoft.OpenApi.dll"
$microsoftOpenApiReadersAssemblyPath = Join-Path $DependentAssembliesDirectoryPath "Microsoft.OpenApi.Readers.dll"
$csharpAnnotationsGenerationAssemblyPath = Join-Path $DependentAssembliesDirectoryPath "Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration.dll"

Load-AssemblyByName "System.Xml.Linq"
Load-AssemblyByPath $sharpYamlAssemblyPath
Load-AssemblyByPath $microsoftOpenApiReadersAssemblyPath
Load-AssemblyByPath $microsoftOpenApiAssemblyPath
Load-AssemblyByPath $csharpAnnotationsGenerationAssemblyPath

#----------------------------------------------------------------------------------------------------------------------
# Ensure the output location exists.
#----------------------------------------------------------------------------------------------------------------------
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath | Out-Null
    Write-Host "Output directoy does not already exist. Created directory $OutputPath"
}

#----------------------------------------------------------------------------------------------------------------------
# Validate input.
#----------------------------------------------------------------------------------------------------------------------
for ($i = 0; $i -lt $VisualStudioXmlDocumentationPaths.Length; $i++) {
    if (-not (Test-Path $VisualStudioXmlDocumentationPaths[$i])) {
        throw [System.IO.FileNotFoundException] "$VisualStudioXmlDocumentationPaths[$i] not found."
    }
}

if ($AdvancedConfigurationXmlDocumentPath -and -not (Test-Path $AdvancedConfigurationXmlDocumentPath)) {
    throw [System.IO.FileNotFoundException] "$AdvancedConfigurationXmlDocumentPath not found."
}

if ($DocumentDescriptionFilePath -and -not (Test-Path $DocumentDescriptionFilePath)) {
    throw [System.IO.FileNotFoundException] "$DocumentDescriptionFilePath not found."
}

for ($i = 0; $i -lt $AssemblyPaths.Length; $i++) {
    if (-not (Test-Path $AssemblyPaths[$i])) {
        throw [System.IO.FileNotFoundException] "$AssemblyPaths[$i] not found."
    }
}

for ($i = 0; $i -lt $FilterPaths.Length; $i++) {
    if (-not (Test-Path $FilterPaths[$i])) {
        throw [System.IO.FileNotFoundException] "$FilterPaths[$i] not found."
    }
}

$openApiSpecificationVersion = $(Get-OpenApiSpecVersion $MajorOpenApiSpecificationVersion $MinorOpenApiSpecificationVersion)

#----------------------------------------------------------------------------------------------------------------------
# Load input dependencies and generate OpenAPI document(s).
#----------------------------------------------------------------------------------------------------------------------
$visualStudioXmlDocuments = @()
for ($i = 0; $i -lt $VisualStudioXmlDocumentationPaths.Length; $i++) {
    $visualStudioXmlDocuments += @(Load-XmlDocument $VisualStudioXmlDocumentationPaths[$i])
}

$advancedConfigurationXmlDocument = $null
if ($AdvancedConfigurationXmlDocumentPath) {
    $advancedConfigurationXmlDocument = Load-XmlDocument $AdvancedConfigurationXmlDocumentPath
}

$documentDescription = $null
if ($DocumentDescriptionFilePath) {
    $documentDescription = Read-FileContent $DocumentDescriptionFilePath
}

$documentGenerationSettings = Create-DocumentGenerationSettings -UseCamelCaseForSchemaProperties:$UseCamelCaseForSchemaProperties

$generationObjects = Generate-OpenApiDocuments `
    $DocumentVersion `
    $visualStudioXmlDocuments `
    $AssemblyPaths `
    $advancedConfigurationXmlDocument `
    $documentGenerationSettings `
    $documentDescription `
    $FilterPaths `
    $FilterClassNames

$variantInfoToDocumentMap = $generationObjects[0]
$generationDiagnostic = $generationObjects[1]

#----------------------------------------------------------------------------------------------------------------------
# Write OpenAPI document(s) to user specified output location.
#----------------------------------------------------------------------------------------------------------------------
if ($variantInfoToDocumentMap) {
    foreach ($keyValuePair in $variantInfoToDocumentMap.GetEnumerator()) {
        Write-OpenApiDocument `
            $OutputPath `
            $keyValuePair.Key `
            $keyValuePair.Value `
            $openApiSpecificationVersion `
            $Format
    }
}

#----------------------------------------------------------------------------------------------------------------------
# Bail with proper exit code when generation fails.
#----------------------------------------------------------------------------------------------------------------------
if ($generationDiagnostic.DocumentGenerationDiagnostic.Errors.Count -gt 0 -or $generationDiagnostic.OperationGenerationDiagnostics.Errors.Count -gt 0) {
    Write-Errors `
        $OutputPath `
        $(Aggregate-GenerationDiagnosticErrors $generationDiagnostic) `
        $MajorOpenApiSpecificationVersion `
        $MinorOpenApiSpecificationVersion

    EXIT $ErrorExitCode
}