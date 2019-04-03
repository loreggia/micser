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
* [CommonServiceLocator](https://github.com/unitycontainer/commonservicelocator)
* [CSCore](https://github.com/filoe/cscore)
* [Hardcodet.NotifyIcon.Wpf](http://www.hardcodet.net/wpf-notifyicon)
* [Newtonsoft.Json](https://www.newtonsoft.com/json)
* [NLog](https://nlog-project.org/)
* [Prism](https://github.com/PrismLibrary/Prism)
* [SQLite.CodeFirst](https://github.com/msallin/SQLiteCodeFirst)
* [System.Data.SQLite](https://system.data.sqlite.org/index.html/doc/trunk/www/index.wiki)
* [System.Data.SQLite.EF6.Migrations](https://github.com/bubibubi/db2ef6migrations)
* [Unity](https://github.com/unitycontainer/unity)
* [WixSharp](https://github.com/oleg-shilo/wixsharp)
* [Windows Driver Samples](https://github.com/Microsoft/Windows-driver-samples)
