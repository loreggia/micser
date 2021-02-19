# Current State
As the initial version did not meet some of my expectations the project is currently undergoing a major refactoring. The main problems I wish to address are:
* Using WPF was not an optimal choice. I struggled to create a good user experience, especially for the main dashboard.
* Communication between the frontend and the service via named pipes caused security problems on some machines.

Additionally, as I currently work mostly in web development and feel more comfortable there, this led me to start a big refactoring process, which involves:

* Recreating the UI as a web app based on React and using the React-Flow library
* Converting the service communications to a REST API

Because of this, the development of new features will be on hold until I reach feature parity with the old architecture.

# Micser
Micser is a modular audio routing application for Microsoft Windows (mostly) written in C#.

The application includes the following parts:
* WPF application with graphical audio routing using widgets
* Audio engine Windows service
* Virtual audio cable driver with a configurable number of devices (WIP, will require an extended validation code signing certificate)

## Building
### Requirements
* Visual Studio 2019
  * .NET Core SDK 3.1
  * Windows 10 SDK, version 10.0.18362.0
  * WDK for Windows 10, version 1903
  * MSVC v142 build tools & spectre-mitigated libs (v14.21)
* WiX Toolset 3.11.1

[![Build Status](https://dev.azure.com/loreggia/micser/_apis/build/status/micser%20CI?branchName=master)](https://dev.azure.com/loreggia/micser/_build/latest?definitionId=4&branchName=master)
![micser CI](https://github.com/loreggia/micser/workflows/micser%20CI/badge.svg?branch=master)

### Installer
Building the installer (Micser.Setup project) and the driver (Micser.Vac.* projects) in Release mode requires the presence of a code signing certificate.
The certificate needs to be named "Certificate.pfx" and placed in the folder "crt" in the repository root.

## Plugins
Micser is a modular framework and can easily be extended with plugins.

The main modules (Device In-/Output, Gain, Compressor, ..) are provided in the Micser.Plugins.Main assembly, which serves as an example for how to implement a plugin for Micser.

## Credits
This project uses the following libraries:
* [CSCore](https://github.com/filoe/cscore)
* [Hardcodet.NotifyIcon.Wpf](http://www.hardcodet.net/wpf-notifyicon)
* [MessagePack-CSharp](https://github.com/neuecc/MessagePack-CSharp)
* [Microsoft.EntityFrameworkCore](https://docs.microsoft.com/ef/core/)
* [Microsoft.EntityFrameworkCore.Sqlite](https://docs.microsoft.com/ef/core/)
* [Microsoft.Xaml.Behaviors.Wpf](https://github.com/Microsoft/XamlBehaviorsWpf)
* [Moq](https://github.com/moq/moq4)
* [Newtonsoft.Json](https://www.newtonsoft.com/json)
* [NLog](https://nlog-project.org/)
* [Prism](https://github.com/PrismLibrary/Prism)
* [Unity](https://github.com/unitycontainer/unity)
* [WixSharp](https://github.com/oleg-shilo/wixsharp)
* [Windows Driver Samples](https://github.com/Microsoft/Windows-driver-samples)
* [WPF Diagram Designer](https://www.codeproject.com/Articles/22952/WPF-Diagram-Designer-Part-1)
* [xUnit.net](https://github.com/xunit/xunit)
