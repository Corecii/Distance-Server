This example shows how to run multiple servers from a single executable.

The executable and plugin dlls have been removed:
* DistanceServer contained `AutoServerLinux.x86_64` and `AutoServerLinux_Data`
* `Plugins/Shared` contained `BasicAutoServer.dll`, `HtmlAgilityPack.dll`, `HttpConsole.dll`, `Vanilla.dll`, and `WorkshopSearch.dll`
* `Plugins/VoteServers` contained `RealmsFilter.dll` and `VoteCommands.dll`

This example is meant to run on Linux. It runs `AutoServerLinux.x86_64` for each directory under `Servers`, setting the `-serverdir` to the directory and the server log to `dir/Server.log`.

The rest of the configuration for each server is handled in the configuration json files.