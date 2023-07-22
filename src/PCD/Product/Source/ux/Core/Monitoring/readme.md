# What is this

This folder contains QoS event classes, which are generated from BOND files.

## How to change events

1. Update BOND file under `Schemas` folder.
1. Run `generate.cmd` batch file.
1. Check in updated BOND and newly generated C# source code.

## Caution

Do not enable auto-generation of QoS events at build time, because this makes Visual Studio rebuild all projects 
every time you use F5/Ctrl-F5.
