@echo off

setlocal enabledelayedexpansion

if ""=="!SslIpAddress!" set SslIpAddress=0.0.0.0

netsh http delete sslcert ipport=!SslIpAddress!:443 > nul

netsh http add sslcert ipport=!SslIpAddress!:443 appid={13b57c1a-74e8-42b1-82dd-91c18110b86e} certhash=%2
