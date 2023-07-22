@echo off

pushd %~dp0

call ..\..\Build\buildenv.cmd

setlocal enabledelayedexpansion
set schemas=

for %%B in (Schemas\*.bond) do (
	set schemas=!schemas! "%cd%\%%B"
)

set ParallaxProject=..\ux.configuration.parallax\generated

if not exist %ParallaxProject% (
	mkdir %ParallaxProject%
)

%NugetPackages%\parallax\%ParallaxPackageVersion%\CodeGenerator\Microsoft.Search.Platform.Parallax.Tools.CodeGenerator.exe --csharpInterfaces --csharpImplementation --workingFolder %ParallaxProject% --schemaFiles %schemas%

if not exist .\generated (
	mkdir .\generated
)

move /y %ParallaxProject%\*.Interface.Generated.cs .\generated
move /y %ParallaxProject%\*.Enum.Generated.cs .\generated

popd
