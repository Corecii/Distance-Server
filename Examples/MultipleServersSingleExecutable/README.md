This example shows how to run multiple servers from a single executable file. This is used in the live distance auto servers.

The executable and plugin dlls have been removed:
* DistanceServer contained `DistanceServer.x86_64` and `DistanceServer_Data`
* `Plugins/Shared` contained `BasicAutoServer.dll`, `HtmlAgilityPack.dll`, `HttpConsole.dll`, `Vanilla.dll`, and `WorkshopSearch.dll`
* `Plugins/VoteServers` contained `RealmsFilter.dll` and `VoteCommands.dll`

This example is meant to run on Linux. It runs `DistanceServer.x86_64` for each directory under `Servers`, setting the `-serverdir` to the directory and the server log to `dir/Server.log`.

The rest of the configuration for each server is handled in the configuration json files.

This server is updated using git with the techniques from [this](https://gist.github.com/Nilpo/8ed5e44be00d6cf21f22) post. Before updating, it kills all instances of the distance server. After updating, it relaunches the distance servers.