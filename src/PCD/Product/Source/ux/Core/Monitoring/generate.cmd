@echo off

pushd %~dp0

call ..\..\..\..\Build\buildenv.cmd

msbuild generate.proj

popd
