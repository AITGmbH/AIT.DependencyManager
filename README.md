# AIT Dependency Manager
This repository contains the sources of [AIT Dependency Manager](http://www.aitgmbh.de/nc/downloads/team-foundation-server-tools/erweitertes-dependency-management-fuer-tfs-und-visual-studio.html).

# License
[Microsoft Public License](LICENSE.txt)

# Building

1) Use Visual Studio 2015
2) Install [Visual Studio 2015 SDK](https://msdn.microsoft.com/en-us/library/mt683786.aspx)
3) Search for the file `Microsoft.VisualStudio.TeamFoundation.dll` inside `C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\Extensions` and copy it to `<REPO_ROOT>\Lib\MS\Visual Studio\14.0`
4) Search for the file `Microsoft.VisualStudio.TeamFoundation.VersionControl.dll` inside `C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\Extensions` and copy it to `<REPO_ROOT>\Lib\MS\Visual Studio\14.0`
5) Install [WiX Toolset](http://wixtoolset.org) 

# Contributing

see [Contributing.md](Contributing.md)

# Debugging

## VS Extensionon on local machine (user-specific settings)
* Open project propteries in Visual Studio of Project `AIT.DMF.DependencyService`
* Choose Start option: Call external program and choose installation folder of devenv.exe (e.g. `C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\devenv.exe`)
* Set `Command Line Arguments` to `/rootsuffix Exp`
* Run Start (F5) and Debug

## MSBuild task on local machine (user-specific settings)
* Open project propteries in Visual Studio of Project `AIT.DMF.MSBuild`
* Choose Start option: Call external program and choose installation folder of msbuild.exe (e.g. `C:\Program Files (x86)\MSBuild\12.0\Bin\MSBuild.exe`)
* Set `Command Line Arguments` to `<REPO_ROOT>\src\MSBuild.targets /t:GetDependencies`
  * MSBuild.targets contains the call of the MSBuild custom task GetDependencies. In this file the path to AssemblyFile must be modified to the local output folder of Dependency Manager
* Set `AIT.DMF.MSBuild` as StartUp Project
* Run Start (F5) and Debug