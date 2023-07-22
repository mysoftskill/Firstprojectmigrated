# Takes in the junit test results from I9n tests and compiles them into nunit format using xslt compiler

$inputFile =$env:WorkingDirectory + "\intTest\test-reports\i9n-tests\junitresults.xml"
Write-Host Input File: $inputFile

$formatFile = $env:WorkingDirectory + "\intTest\CloudTest\Converter\junit-to-nunit.xsl"
Write-Host Format File: $formatFile

$outputFile = $env:LoggingDirectory + "\testResult.xml"
Write-Host Output File: $outputFile

$XSLInputElement = New-Object System.Xml.Xsl.XslCompiledTransform
$XSLInputElement.Load($formatFile)

$XSLInputElement.Transform($inputFile, $outputFile)
Write-Host Nunit File at $outputFile
