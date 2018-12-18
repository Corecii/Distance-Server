
# Plugin Development Setup

## Prerequisites

1. Visual Studio 2017
2. Git
3. Unity Editor

## Suggested Setup for separate plugin projects

1. Make a directory for your plugin
2. Initialize git in the directory using `git init`
3. Add the Distance Standalone Server as a git submodule using `git submodule add https://github.com/Corecii/Distance-Server DistanceServerStandalone`
4. Initialize the submodule using `git submodule update --init --recursive`
5. Add a patched `Distance.dll` to the Distance Standalone Server directory
6. In Visual Studio, create a new solution in your plugin directory
7. Add `Assembly-CSharp.csproj`, `Assembly-CSharp-firstpass.csproj`, and `DistanceServer.csproj` to your solution
8. If you plan on using any plugins, add their projects to your solution
9. In your solution, add a C# Class Library `.NET Framework 4.6.1` project
10. Add references to the other added projects
11. Add a reference to `Distance.dll`, and give it the `Distance` alias instead of `global`. If you need to access it in a file, you must add `extern alias Distance;` to the top of the file.
12. Add a post-build event to copy the plugin files to the server directory: `xcopy "$(TargetDir)$(TargetName).*" "$(SolutionDir)DistanceServerStandalone/DistanceServer/Plugins" /Y`
13. Write your plugin. Building it shold copy the dll files to the test server's plugin directory, which you can run in the Unity Editor to test your plugin.

A git submodule is used so that the exact version of DistanceServerStandalone is tracked and easy for other contributors to grab. This makes it easy for others to work on your project and easy for you to come back to. This also makes upgrading easy: pull the new version of DistanceServerStandlone, then update all of your code for the new version.

## Setup for primary plugins which are bundled with DistanceServerStandalone

1. Open the server solution
2. Do steps 9 through 13 above. Use `xcopy "$(TargetDir)$(TargetName).*" "$(SolutionDir)Plugins" /Y` instead.

## Alternatives

You don't need git or Unity Editor, and you can probably do without Visual Studio or use a different version.

### Without git or git submodules

Download DistanceServerStandalone, and reference the projects as you normally would. Modify the xcopy command to copy to the correct directory.

### Without downloading the project

Download the compiled server and plugin dlls, and generate a patched `Distance.dll`. Reference the dlls directly instead of the projects.

### Without Unity Editor

Download the server, and set xcopy to copy to the server's plugins directory. Launch the server, and read its log file for debugging.

### Without xcopy

Copy manually, or use a different copying executable.

### Without Visual Studio

Add references to the required DLLs, as in "Without downloading the project". Copy your compiled dlls to the server's plugins directory.

# Plugin Development

#### Loading

The distance server first loads all plugin dlls, then sorts them by their `Priority` and loads them.

#### Plugin entry points

The distance server will find all classes in the plugin which are subclasses of `DistanceServerPlugin`, are not abstract, and have an empty constructor. It makes one of each it finds and calls `Start` on each.

Since the distance server can load multiple entry points per plugin, you can specify multiple entry points with different priorities so that your plugin can load before *and* after other plugins, if needed.

#### Cross-plugin communication

The distance server provides a `T GetPlugin<T>()` method, which returns the loaded instance of the given plugin type `T`. Add a reference to the other plugin type, and use `GetPlugin<T>` to get its plugin instance. Alternatively, just add a reference to the other plugin dll and use its static methods. Since the distance server loads all plugins before calling any plugin code, you should always get the already-loaded plugin.

The BasicAutoServer plugin provides methods to customize server behavior, filter levels, and set a custom playlist. The WorkshopSearch plugin provides methods to search the workshop by parsing the user-facing workshop pages.