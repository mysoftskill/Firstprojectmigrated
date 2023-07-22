:: Restore nuget packages used by OpenAPI tool

setlocal

:: This will also get all the dependencies, such as Microsoft.OpenApi.
nuget install Microsoft.OpenApi.CSharpAnnotations.DocumentGeneration -Version 2.0.0-beta02 -OutputDirectory %~dp0\openapi

:: Flatten the binaries by copying all .Net Framework dll to the same folder
for /f %%i in ('dir /b /s %~dp0\*.dll ^| findstr \net46\') do copy %%i %~dp0 /Y
for /f %%i in ('dir /b /s %~dp0\*.dll ^| findstr \net45\') do copy %%i %~dp0 /Y

rmdir %~dp0\openapi /s /q
