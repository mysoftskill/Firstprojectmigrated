# Creating Documentation

## VSTS Markdown Support

VSTS has support for advance [markdown](https://docs.microsoft.com/en-us/vsts/collaborate/markdown-guidance?view=vsts).

## PlantUML

[PlantUML](https://plantuml.com/) is a tool for generating UML diagrams from human readable plain text files.

### Installing PlantUML

The easiest way (to me at least) to install plantuml is to use [chocolatey](https://chocolatey.org/).

1. Follow instructions to install chocolatey [here](https://chocolatey.org/install).
2. From an elevated command prompt run:
   `choco install plantuml`

### Generating Images

In command prompt run:
`plantuml .\path\to\umlfile.wsd -tpng -o .\path\to\output`
**Note: the output path is relative to the source directory**

This tells plantuml to use the input files and generate png image files in the output directory.

The files can then be referenced using the image syntax in markdown:
`![alt text](./path/to/image.png)`

## Pandoc

[pandoc](https://pandoc.org/) is a tool for converting documents from one type to another. It supports going from
markdown to word.

### Installing Pandoc

The easiest way (to me at least) to install plantuml is to use [chocolatey](https://chocolatey.org/).

1. Follow instructions to install chocolatey [here](https://chocolatey.org/install).
2. From an elevated command prompt run:
   `choco install pandoc`

### Generating a Word Doc

Pandoc takes the markdown as an argument as well as where to output the word document:
`pandoc .\MarkdownFile.md -f markdown --toc -o .\word\doc\to\Create.docx`

What's this doing:

1. `pandoc .\MarkdownFile.md` tells it the input file
2. `-f markdown` tells it that it's a markdown file (since markdown is a plain text document)
3. `--toc` tells it to generate a table of contents based on the headings
4. `-o .\word\doc\to\Create.docx` tells it where to write the generated word document