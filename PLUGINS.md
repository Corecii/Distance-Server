# Distance Server Plugins

## BasicAutoServer

The basic auto server runs through either the campaign levels or through workshop levels retrieved through multiple combined searches.

The basic auto server can be customized using a `BasicAutoServer.json` file in the server config directory:

| Setting and Default Value | Description |
| :------ | :---------- |
| `"ServerName": "Auto Server"` | The name of the server, which shows in the lobby and in the server list. |
| `"MaxPlayers": 24` | The maximum number of players. |
| `"Port": 45671` | The port for the server. The server can be connected to from the server list regardless of what port you choose. You can run multiple servers on one machine by choosing different ports for each. |
| `"ReportToMasterServer": true` | Whether this server should show on the server list. |
| `"MasterServerGameModeOverride": "None"` | If present, changes the text that appears for the game mode on the server list. Otherwise, the text for the gamemode (e.g. "Sprint") is used. |
| `"WelcomeMessage": "None"` | If present, shows a welcome message to players when they join the game. |
| `"LevelTimeout": 420` | The time in seconds after which a level will be ended automatically. |
| `"IdleTimeout": 60` | The time in seconds after which a player will be set to DNF if they have not moved. |
| `"LoadWorkshopLevels": false` | Whether the server should search and load workshop levels. If set to true, requires the `WorkshopSearch` plugin. |
| `"Workshop": []` | A list of workshop level searches to retrieve workshop levels from. |

An example `"Workshop"` setting:
```json
"Workshop": [
		{
			"Search":"",
			"SearchType":"GameFiles",
			"Sort":"Popular",
			"Days":30,
			"Tags":["Sprint"],
			"Count":10
		},
		{
			"Search":"",
			"SearchType":"GameFiles",
			"Sort":"Popular",
			"Days":360,
			"Tags":["Sprint"],
			"Count":30
		},
		{
			"Search":"",
			"SearchType":"GameFiles",
			"Sort":"Subscribed",
			"Days":-1,
			"Tags":["Sprint"],
			"Count":200
		},
		{
			"Search":"",
			"SearchType":"GameFiles",
			"Sort":"Popular",
			"Days":-1,
			"Tags":["Sprint"],
			"Count":200
		}
]
```

Workshop search parameters:

| Parameter | Description | Options |
| :-------- | :---------- | :------ |
| Search | Search text | |
| SearchType | Type of search to make. Right now, only GameFiles is supported. In the future, GameCollections, UserFiles, UserCollections, and CollectionFiles may be supported. | GameFiles |
| Sort | The type of sort to use. | Default |
| | | Popular |
| | | Playtime |
| | | Recent |
| | | Subscribed |
| | | Relevance |
| Days | The days to use in the sort. `-1` is used for all-time. | |
| Tags | The required tags | Casual |
| | | Normal |
| | | Advanced |
| | | Expert |
| | | Nightmare |
| | | Sprint |
| | | Reverse+Tag |
| | | Stunt |
| | | Trackmogrify |
| | | Main+Menu |
| | | Survive+the+Editor |
| | | Distance+Advent+Calendar+2014 |
| | | Distance+Advent+Calendar+2015 |
| | | Distance+Advent+Calendar+2016 |
| | | Distance+Advent+Calendar+2017 |
| | | Distance+Advent+Calendar+2018 |
| Count | The maximum number of levels to grab | |

Levels will be mixed based on their count. If you have 100 levels from search B, and 10 from search A, then 1 level from search A will appear after every 10 levels from search B.

## Vote Commands

Vote commands requires `BasicAutoServer` and `WorkshopSearch` and allows your players to vote for any track from the workshop.

Each players can vote for one track at a time. When the level advances, VoteCommands will choose randomly from every player's picks. Multiple players voting for the same level makes it *more likely* but not *guaraneteed* to be chosen. Once VoteCommands has picked your level, you vote is cleared and you can vote for another. You can replace or clear your vote at any time. You vote will stick around for 5 minutes after you've left, but cannot be chosen if you are not in the game. This allows you to reconnect to the server without losing your vote.

Additionally, VoteCommands allows players to vote to skip a level.

Vote commands can be customized using `VoteCommands.json` in the server config directory:

| Setting and Default Value | Description |
| :------ | :---------- |
| `"SkipThreshold": 0.7` | The threshold to skip a level. The default, `0.7`, means that 70% or more players must vote to skip the level to move on. Set this to `-1` to disable vote-skipping. |
| `"RequiredTags": ["None"]` | List of tags. Players can only search for levels that match one of the given tags. If not present, players can vote for any Sprint level. |

All players can use all commands.

| Commands | Description |
| :------ | :---------- |
| `/help` | Displays "/search /vote /skip" |
| `/search Level name` | Searches for all levels with the given name. Uses the default steam workshop search, so you must use full words. |
| `/search Level name by author` | Searches for all levels with the given name by a specified author. "by author" should not be used alone, as it **will not** find *all* levels by the given author. |
| `/vote Level name by author` | Votes for a level, using the same syntax as `/search` |
| `/skip` | Vote or unvote to skip the current level. |

## RealmsFilter

Realms filter requires `BasicAutoServer` and `WorkshopSearch`.

The realms filter will filter Realms and Hot Wheels Acceleracers tracks either in or out. It will filter both the auto-retrieved levels and voted-for levels.

Realms filter can be customized using `RealmsFilter.json` in the server config directory:

| Setting and Default Value | Description | Options |
| :------ | :---------- | :---- |
| `"FilterMode": NoRealms` | How to filter levels | |
| | If set, does not allow levels with "acceleracer", "realm", or "hot wheels" in their names. | NoRealms |
| | If set, only allows levels with "acceleracer", "realm", or "hot wheels" in their names. Does not allow levels with a rating below 4, or levels with "old", "lamp", or "meme" in their names.| RealmsOnly |
| | If set, only allows levels with 4 or more stars. | GoodEasyOnly |

## HttpConsole

Http console lets you view server info through a basic web server. This is a proof-of-concept for a commandline console, web console, or other server management console. Since https is not supported, HttpConsole has no authentication mechanism as it has no protection for man-in-the-middle attacks. Because of this, it has a public mode switch that hides sensitive data.

Http console can be customized using `HttpConsole.json` in the server config directory.

| Setting and Default Value | Description |
| :------ | :---------- |
| `"Port": 45681` | The port to access the server on. |
| `"LogFileLocation": "path/to/log file"` | The path to the log file, `null` by default. If provided, lets you read and download the log file if not in public mode. If not provided, the server will try to find the log file by looking in the default locations. |
| `"PublicMode": false"` | Whether public mode is on or off |

| Command | Description |
| :------ | :---------- |
| `/players` | Views a players list |
| `/players detailed` | |
| `/level` | Views the current level |
| `/info` | Views general info |
| `/chat` | Views recent chat messages |
| `/chat message` | Says a chat message, if not in public mode |
| `/log` | Views the server debug log , if not in public mode |
| `/summary` | Displays server info, current level, players, and chat |
| `/summary detailed` | Same as `/summary`, but with a detailed players list |

## WorkshopSearch

WorkshopSearch allows other plugins to search the workshop. It parses the user-facing workshop pages.

This plugin offers no configuration. This plugins requires `HtmlAgilityPack.dll`, which is used to parse the returned workshop results.