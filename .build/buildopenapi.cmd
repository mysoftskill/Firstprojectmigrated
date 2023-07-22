:: Generate OpenApi document

if "%CDP_FILE_VERSION_NUMERIC_NOLEADINGZEROS%"=="" (
    set CDP_FILE_VERSION_NUMERIC_NOLEADINGZEROS=0.0.851.2
)

powershell.exe -NoProfile -ExecutionPolicy Unrestricted -Command "& %~dp0\GenerateOpenApiDoc.ps1 -RepoRoot %~dp0.. -BuildVersion %CDP_FILE_VERSION_NUMERIC_NOLEADINGZEROS%" -ErrorAction Stop || exit /b 1
