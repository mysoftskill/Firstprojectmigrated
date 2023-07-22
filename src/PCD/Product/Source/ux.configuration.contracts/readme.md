# What is this

This folder contains contracts for Parallax configuration sections, which are generated from BOND files.

## How to change contracts

1. Update BOND file under `Schemas` folder.
1. Run `generate.cmd` batch file.
1. Post PR with updated BOND and newly generated C# source code from `ux.configuration.contracts` and 
`ux.configuration.parallax` projects.
1. Check in all the above files.

## Caution

Do not enable auto-generation of configuration contracts at build time, because this makes Visual Studio rebuild all projects 
every time you use F5/Ctrl-F5.
