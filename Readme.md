# Micser
Micser is a modular audio routing framework for Microsoft Windows mostly written in C#.

## FAQ

## Building
### Requirements
* Visual Studio 2017
* Windows Driver Kit 10.0.17763.1
* WiX Toolset 3.11.1

## Plugins
Micser is a modular framework and can easily be extended with plugins.

The main modules (Device In-/Output, Gain, Compressor) are provided in the Micser.Plugins.Main assembly, which serves as an example for how to implement a plugin for Micser.

## Credits
This project uses the following libraries:
* Prism.Wpf
* Prism.Unity
* CommonServiceLocator
* Unity
* CSCore
* WixSharp
* NLog
* System.Data.SQLite
* System.Data.SQLite.Core
* System.Data.SQLite.EF6.Migrations
* SQLite.CodeFirst
* Hardcodet.NotifyIcon.Wpf
* Newtonsoft.Json