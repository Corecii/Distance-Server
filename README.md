# Distance Standalone Server

## Setup

Grab the Server release from the Releases page. Unzip it to anywhere you want.

### Patching

The server needs a patched version of Distance's code to run:

1. Grab the Patcher release from the Releases page.
2. Open up your copy of Distance. If you got Distance through Steam, this will be at `C:\Program Files (x86)\Steam\steamapps\common\Distance`. If otherwise, then you should know its install location.
3. Navigate to the `Distance\Distance_Data\Managed` directory.
4. Drag `Assembly-CSharp.dll` to `Patcher.exe`.
5. Drag `Distance.dll` to the Server directory.

### Config

Server launch arguments:

| Argument | Description |
| :------- | :------ |
| `-serverdir` | Sets the server config directory. The server config directory houses all setting files for a server. If not set, then the server executable directory is used. |
| `-nodefaultplugins` | By default, the server will load all plugins in `server executable directory/Plugins`. This turns that off. |
| `-noserverplugins` | By default, the server will load all plugins in `server config directory/Plugins`. This turns that off. |
| `-plugin <plugin file path>` | Loads a specific plugin. |
| `-pluginsdir <plugin dir path>` | Loads all plugins in the specified directory. |
| `-loadfrom` | Uses an alternative method to load plugin dlls. Not recommended. |
| `-batchmode -nographics` | Unity args on Windows to run the server without graphics. You will have to manually kill the process through the task manager. |
| `-logFile` | Unity arg to set log file location. It is recommended to set this as the same as your server config directory. |

The server will read base server settings from a file in the server config directory names `Server.json`:

| Setting | Description |
| :------ | :------ |
| `"PluginsDir": ["path/to/directory", "path/to/directory"]` | Include plugins from multiple directories |
| `"Plugins": ["path/to/plugin", "path/to/plugin"]` | Include plugins from multiple files |

Additional configuration can be done through plugins that read config files or apply hardcoded settings.

### Plugins

The server supports plugins. Plugins are intended to work like scripts that use the server's API to customize server functionality. Most plugins provide limited customization through config files, and extended customization can be done by modifying the plugin code.

Default plugins

| Plugin | Description |
| :----- | :---------- |
| Vanilla | A plugin to provide vanilla functionality, such as some messages and countdowns. |
| BasicAutoServer | A plugin to run a server over a set of maps continuously. By default it loops through all campaign maps, and can be configured to build a list of levels using Steam Workshop searches. |
| VoteCommands | A plugin to let players vote to play workshop levels. Votes work in a Mario Kart-like manner: as if a wheel spins and chooses a level, so more votes makes a level more likely, but any voted-for level may be chosen. |
| WorkshopSearch | A plugin that allows other plugins -- such as BasicAutoServer and VoteCommands -- to search the Steam Workshop. This plugin does not provide configuration. |
| RealmsFilter | A plugin to filter in/out Realms maps. Since these two map types are fundamentally different and since the community is split between both map types, it is good to keep the two separate to keep both groups happy and prevent vote trolling. |
| HttpConsole | A plugin to provide information about the server and its players over a small web server. This provides no method for authentication, so configuration allows for limiting displayed info for privacy. |

See [`PLUGINS.md`](PLUGINS.md) for plugin configuration and usage.

See [`Examples/PluginConfiguration`](Examples/PluginConfiguration) for example plugin configuration.

See the [`PLUGINDEV.md`](PLUGINDEV.md) for info on developing your own plugins.

### Running

On Windows, the server provides a basic UI unless launched with `-batchmode -nographics`. If the executable directory is used as the config directory, then you can double click the exe to launch the server.

On Linux, the server does not provide a UI. You can check the server log by opening the file or using the HttpConsole plugin. The server can be closed with `pkill`, other task killing commands, or a task manager equivalent.

An example script to run multiple servers from multiple directories is provided in [`Examples/MultipleServersSingleExecutable`](Examples/MultipleServersSingleExecutable).

### Easy Quick-Start

1. Add a `Plugins` folder next to `AutoServer.exe`
2. Add the plugins you want to use to the `Plugins` folder
3. Add the default config files for plugins next to `AutoServer.exe`
4. Run `AutoServer.exe`. The plugins in `Plugins` will be loaded by default and use the settings from your config files.

## Contributing

### Setup

The server, plugins, patcher, and network debugger can be opened using Visual Studio 2017.

Since this project uses git submodules, you can either do your initial clone of the project using:

```
git clone --recursive https://github.com/Corecii/Distance-Server
```

Or you can clone the repository, then init all submodules:

```
git clone https://github.com/Corecii/Distance-Server
git submodule update --init --recursive
```

### Network Debugger

The network debugger is a [Spectrum](https://github.com/Ciastex/Spectrum) mod that...
* Prints out all global events and their indexes.
* Prints out all instanced events and their indexes.
* Prints out whenever it receives an event of any type, with the associated data for that event call.
* Prints out a list of all official levels, for use in making the default playlist.

The network debugger can be used to update the events lists, investigate unimplemented server functionality, and rebuild the default campaign playlist.

### Server Additions

When new functionality is implemented, it should go...
* In the base server, if it involves common network routines or an implementation of default distance data types and networking processes. (e.g. the process of adding a player to the game)
* In the Vanilla plugin, if it involves optional, but default behaviors such as messages, timers, or game modes that Distance uses. (e.g. messages, end-of-level countdown, and the sprint, tag, and stunt gamemodes)
* In other plugins or a new plugin, it it involves optional additional functionality (e.g. auto servers, tournament servers, user interface, map voting)

The server is meant to be used with plugins in all scenarios, so don't shy away from implementing additional functionality in plugins. The base server should be very lightweight and offer little functionality without additional scripting (i.e. plugins).

If you would like to provide the server in an easy-to-use package, then it is reasonable to bundle the base server with plugins and a launcher/UI without modifying the base server directly.

### Testing

The server is set up to make testing easy. Each plugin copies its files to the default Plugins directory when built. The external server dll copies to the executable directory when built. The server project comes with configuration files that make testing the plugins and the servee as a whole easy.

Build all the plugins and the external server in Visual Studio. Run the server in Unity. The server should load all plugins and their config files into a local server that does not report to the master server. You can test it by joining `127.0.0.1` or your local IP address if connecting from another local computer.