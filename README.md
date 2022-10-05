# Installation:
- Download [Bepinex](https://builds.bepinex.dev/projects/bepinex_be/577/BepInEx_UnityIL2CPP_x64_ec79ad0_6.0.0-be.577.zip).
- Open your game folder (In steam, right click crab game, Manage > Browse local files).
- extract Bepinex so that all of it's files are in the game folder.
- run the game once. This will take some time.
- close the game.
- download `MapMod.dll` from [Releases](https://github.com/o7Moon/CrabGame.MapMod/releases).
- move `MapMod.dll` to `(Game Folder from step 2)/BepInEx/plugins/MapMod.dll`.

# FAQ
### how do i play custom maps?
when playing in somebody else's lobby, the mod will handle map installation automatically. if you are just testing the mod, you have a few options for loading maps:
- create a folder inside your game folder called `TestMaps`, and place the map that you want to try into that `TestMaps` folder. The practice mode UI will show a button for the map, but only once you actually start a lobby and pause.
- put the map in the `(Game Folder)/Maps` folder, add it's folder name to `(Game Folder)/maps.txt`, and make sure `(Game Folder)/hostindex.txt` says `useLocal`. this will add the map to the game's registry and allow you to select it when starting a lobby. this *does not*, however, allow other players to see the map when they join your lobby. to do that you need to set up a map index or use an existing one, more info on that below.
### where are all the lobbies?
the issue described below has been fixed in 0.6.1. if you still have the issue, you can grab the update from [Releases](https://github.com/o7Moon/CrabGame.MapMod/releases).
~~crab game detects mods and only lets you play in lobbies hosted by other players with mods. if you would like to play unmodded lobbies, move the `BepInEx` folder and `winhttp.dll` file out of your game folder (I have a folder called `Game Folder/mod_storage` that i move them to) to temporarily disable mods.~~
### what are map indexes and how do i use them?
map indexes are lists of maps that the mod can download automatically in the background. each lobby uses exactly 1 index, and you can think of it as that lobby's "map pack". if you are joining somebody else's lobby, you dont need to worry about any of this. but if you want to host a lobby yourself, there are a few things you need to know to get indexes working:
1. The index that everyone uses when they join your lobby is called the "host index". you can set the link for this in the `(Game Folder)/hostindex.txt` file, which by default is `useLocal` meaning that you and everyone playing will use their own `Maps` folder and `maps.txt` file. this is usually not what you want since the other players could have different maps installed or have their maps in a different order. instead, you can set a link to an index that everyone should download maps from, and the mod will handle everything else. an example index link is `https://github.com/o7Moon/CrabGame.MapMod/raw/main/index/`.
2. if you want to create your own index, there is a very specific format for doing so:
- indexes must be `github.com` links. this means you'll want to create a github repo [(guide here)](https://docs.github.com/en/get-started/quickstart/create-a-repo) and add your index files to it [(guide here)](https://stackoverflow.com/questions/40720141/how-to-upload-folders-on-github). the link will be something like: `github.com/yourname/yourRepoName/raw/main/`. the reason the example link above also has `/index/` on the end is because that index is part of this main mapmod repository and exists in a subdirectory. you don't have to do this (you can just use the `.../raw/main/` index url and put your indexfile directly in the root of the repository) but you can if you have a map index associated with another project and want to host them both in the same repository.
- `(index)/indexfile` contains a list of maps in the format `version|mapname` on each line. if you update a map, you must also change the version so that the mod knows to update it.
- `(index)/maps/(mapname).zip` must exist for every map and is a zip archive of the map folder (make sure it does not contain another folder within, map.obj needs to be in the top layer of the zip).
### how do i make a map?
there is a [wiki](https://github.com/o7Moon/CrabGame.MapMod/wiki/Map-Creation), but if you need further help (the wiki is not very in depth or very up to date) then feel free to join the [Crab Game Modding Discord](https://discord.gg/SXGHaN8Geb) and ask for help there.