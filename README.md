# MinecraftLogAnalyzer

A command line utility that extracts playerdata from log messages. All data will be saved in json files.

## Current Features
- Playtime
- ChatMessages
- Advancements
- Kills/Deaths
- Commands

## Usage
1. Download the latest release and extract the files.
2. Open a terminal window in the directory with the program files
3. Run `MinecraftLogAnalyzer.exe <Server Log Directory>`
4. The data will be saved in `<Server Log Directory>\export`

## Commandline Arguments
- `-n`: NoExport the data will not be saved but you can still view it in console window
- `--exportDir <ExportDir>`: Lets you specify a directory where the data will be saved
- `--help`: Shows helpfull information about the program
