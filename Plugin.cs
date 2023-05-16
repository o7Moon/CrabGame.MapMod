using BepInEx;
using BepInEx.IL2CPP;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Dummiesman;
using UnhollowerRuntimeLib;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO.Compression;
using System.IO;
using ServerSend = MonoBehaviourPublicInInUnique;

namespace MapMod
{
    [BepInPlugin("CrabGameMapMod", "MapMod", "0.7")]
    public class Plugin : BasePlugin
    {
        public static void Setup(){
            string mapsFolder = System.IO.Directory.GetParent(Application.dataPath) + "\\Maps";
            if (!System.IO.Directory.Exists(mapsFolder)) {
                System.IO.Directory.CreateDirectory(mapsFolder);
            }
            gameFolder = System.IO.Directory.GetParent(Application.dataPath).FullName;
            string hostindexConfigPath = gameFolder+"\\hostindex.txt";
            if (File.Exists(hostindexConfigPath)){
                hostIndex = File.ReadAllText(hostindexConfigPath).Trim();
                // make sure there is actually a path seperator at the end of the index link
                if (hostIndex != "useLocal" && !hostIndex.EndsWith("/")){
                    hostIndex += "/";
                }
            } else {
                File.WriteAllText(hostindexConfigPath,"useLocal");
            }
            indexUrl = hostIndex;
            steamManager = GameObject.Find("/Managers/SteamManager").GetComponent<MonoBehaviourPublicObInUIgaStCSBoStcuCSUnique>();
            hostID = steamManager.prop_CSteamID_0;
            mapManager = GameObject.Find("/Managers/Map&GameModes").GetComponent<MonoBehaviourPublicObInMamaLi1plMadeMaUnique>();
            gamemodeManager = GameObject.Find("/Managers/Map&GameModes").GetComponent<MonoBehaviourPublicGadealGaLi1pralObInUnique>();
            defaultMaps = mapManager.maps.ToList();
            updateIndex().Wait();
            if (deselectAllMaps.Value){
                mapManager.playableMaps = new Il2CppSystem.Collections.Generic.List<Map>();
            }
            registerMaps();
        }
        public static void registerMaps(){
            var mapList = new System.Collections.Generic.List<Map>(defaultMaps);
            int mapnum = 62;
            foreach (Map m in customMaps){
                m.id = mapnum;
                string mapfolder = currentIndexFolderPath + m.name;
                Texture2D thumbnail = new Texture2D(1,1);
                if (System.IO.File.Exists(mapfolder+"\\map.png")){
                    ImageConversion.LoadImage(thumbnail,System.IO.File.ReadAllBytes(mapfolder+"\\map.png"));
                }
                m.mapThumbnail = thumbnail;
                if (System.IO.File.Exists(mapfolder+"\\map.config")){
                    string config = System.IO.File.ReadAllText(mapfolder+"\\map.config");
                    string size = tryGetValue(config,"size");
                    if (size != null){
                        switch (size) { 
                            case "large":
                                m.mapSize = Map.EnumNPublicSealedvasmmelaan5vUnique.large;
                                break;
                            case "medium":
                                m.mapSize = Map.EnumNPublicSealedvasmmelaan5vUnique.medium;
                                break;
                            case "small":
                                m.mapSize = Map.EnumNPublicSealedvasmmelaan5vUnique.small;
                                break;
                            default:
                                m.mapSize = Map.EnumNPublicSealedvasmmelaan5vUnique.any;
                                break;
                        } 
                    } else {
                        m.mapSize = Map.EnumNPublicSealedvasmmelaan5vUnique.any;
                    }
                }
                mapnum ++;
                mapList.Add(m);
                // this will ONLY happen on first map register, with the host index. 
                // when playing as a client it is not required to have the correct supoported maps list
                // since the host handles it. because of that its possible to just add these once for the host index
                // and never reset them.
                if (!addedGamemodeSupportedMaps){
                    registerGamemodeSupportedMap(m);
                }
                if (selectCustomMaps.Value){
                    mapManager.playableMaps.Add(m);
                }
            }
            mapManager.maps = mapList.ToArray();
            addedGamemodeSupportedMaps = true;
        }
        public static void registerGamemodeSupportedMap(Map m){
            string mapfolder = currentIndexFolderPath + m.name;
            if (System.IO.File.Exists(mapfolder+"\\map.config")){
                string[] gamemodes = new string[gamemodeManager.allGameModes.Length];
                for (int i = 0; i < gamemodeManager.allGameModes.Length; i++) {
                    gamemodes[i] = gamemodeManager.allGameModes[i].modeName.ToLower();
                }
                string config = System.IO.File.ReadAllText(mapfolder+"\\map.config");
                string mapModes = tryGetValue(config, "modes");
                if (mapModes != null)
                {
                    for (int i = 0; i < gamemodes.Length; i++)
                    {
                        string mode = gamemodes[i];
                        if (mapModes.Contains(mode)) {
                            gamemodeManager.allGameModes[i].compatibleMaps = gamemodeManager.allGameModes[i].compatibleMaps.AddItem(m).ToArray();
                        }
                    }
                }
            }
        }
        // values in object names are formatted valueName[value]
        public static string tryGetValue(string name,string valueName) {
            if (!name.Contains(valueName))
                return null;
            try {
                string valueNameWhole = valueName + "[";
                int valueIndex = name.IndexOf(valueNameWhole) + valueName.Length + 1;
                int valueEndIndex = name.IndexOf("]", valueIndex);
                return name.Substring(valueIndex, valueEndIndex - valueIndex);
            } catch {
                return null;
            }
        }
        public static Vector3 parseVector(string text) {
            string[] array = text.Split(",");
            try {
                return new Vector3(float.Parse(array[0]), float.Parse(array[1]), float.Parse(array[2]));
            } catch { return new Vector3(0,1,0); }
        }
        public static string[] getCustomMapNames() {
            // this is only used when the index is set to "useLocal"
            string path = System.IO.Directory.GetParent(Application.dataPath) + "\\Maps.txt";
            if (!System.IO.File.Exists(path)) {
                System.IO.File.WriteAllText(path," ");
                return null;
            }
            return System.IO.File.ReadAllLines(path);
        }
        public static Map createMap(string name){
            Map map = ScriptableObject.CreateInstance<Map>();
            map.name = name;
            map.mapName = name;
            return map;
        }

        // im a dumbass so ill just add a variable for the dorm thing :thumbs_up:
        public static Map dormMap;
        public static bool loadingCustomMap = false;
        public static bool currentlyPlayingCustomMap = false;
        public static int lastCustomMapID = -1;
        public static bool allObjectsTextured = false;
        public static bool gameLoaded = false;
        public static bool addedGamemodeSupportedMaps = false;
        public static string customMapPath;
        public static string customMapName;
        public static string gameFolder;
        public static int timeLastSentInstallLink = 0;
        // the index url to use when hosting a game. this is configured in (Game Folder/hostindex.txt)
        // if it is set to "useLocal", use Maps.txt and the maps folder instead of an index repo.
        // note that this means clients will also use their own maps
        public static string hostIndex = "useLocal";
        public static SteamworksNative.CSteamID hostID = (SteamworksNative.CSteamID)0;
        public static MonoBehaviourPublicObInUIgaStCSBoStcuCSUnique steamManager;
        public static MonoBehaviourPublicObInMamaLi1plMadeMaUnique mapManager;
        public static MonoBehaviourPublicGadealGaLi1pralObInUnique gamemodeManager;
        public static System.Collections.Generic.List<Map> defaultMaps;
        public static System.Collections.Generic.List<Map> customMaps;
        public static string indexUrl; // eg: https://github.com/o7Moon/CrabGame.MapMod/raw/main/index/ and indexfile and maps/ should exist at this url

        public static string currentIndexFolderPath;

        public static System.Collections.Generic.List<bool> mapsNeedUpdating = new System.Collections.Generic.List<bool>();
        public static System.Collections.Generic.List<System.Func<GameObject,Mesh,bool>> mapLoaderActions = new System.Collections.Generic.List<System.Func<GameObject, Mesh, bool>>();
        
        public static void trySendInstallMessage(){
            if (System.DateTimeOffset.Now.ToUnixTimeSeconds() > timeLastSentInstallLink + 30){
                // ID 1 is server message
                MonoBehaviourPublicInInUnique.SendChatMessage(1,"Hey, this lobby has custom maps and it seems you dont have the mod installed,");
                MonoBehaviourPublicInInUnique.SendChatMessage(1,"join the discord for help with installation: "+discordLink.Value);
                timeLastSentInstallLink = (int)System.DateTimeOffset.Now.ToUnixTimeSeconds();
            }
        }
        public static void registerLoaderAction(System.Func<GameObject,Mesh,bool> action) {
            mapLoaderActions.Add(action);
        }
        public static void onSceneLoad(Scene scene, LoadSceneMode mode) { 
            if (!gameLoaded) {
                gameLoaded = true;
                Setup();
            }
            currentlyPlayingCustomMap = false;
            // once we load a scene, if this load was caused by pressing a custom map button then delete the default map and add our own
            if (loadingCustomMap && scene.name != "LoadingScreen") {
                loadingCustomMap = false;
                currentlyPlayingCustomMap = true;
                if (lastCustomMapID != -1){
                    GameObject.Find("/Managers").GetComponent<MonoBehaviourPublicCSDi2UIInstObUIloDiUnique>().map = mapManager.maps[lastCustomMapID];
                }
                // make spawning consistent
                GameObject.Find("/SpawnZoneManager").transform.GetChild(0).GetComponent<MonoBehaviourPublicVesiUnique>().size = new Vector3(2,2,2);
                GameObject.Destroy(GameObject.Find("/Map"));
                GameObject.Destroy(GameObject.Find("===AMBIENCE==="));
                string mapConfigPath = customMapPath + "\\map.config";
                if (System.IO.File.Exists(mapConfigPath))
                {
                    string config = System.IO.File.ReadAllText(mapConfigPath);
                    allObjectsTextured = false;
                    if (config.Contains("allObjectsTextured"))
                        allObjectsTextured = true;
                    string lightIntensity = tryGetValue(config, "lightingIntensity");
                    if (lightIntensity != null) {
                        Light light = GameObject.Find("Directional Light").GetComponent<Light>();
                        try {
                            light.intensity = float.Parse(lightIntensity);
                        } catch (System.Exception e){
                            instance.Log.LogInfo(e);
                        }
                    }
                    string skycolor = tryGetValue(config, "skycolor");
                    if (skycolor != null){
                        Color skyColor = new Color();
                        ColorUtility.TryParseHtmlString(skycolor, out skyColor);
                        Cubemap sky = new Cubemap(1,UnityEngine.Experimental.Rendering.DefaultFormat.LDR,UnityEngine.Experimental.Rendering.TextureCreationFlags.None);
                        for (int i = 0; i < 6; i++){
                            sky.SetPixel((CubemapFace)i,0,0,skyColor);
                        }
                        sky.Apply();
                        Material skyMaterial = new Material(Shader.Find("Skybox/Cubemap"));
                        skyMaterial.SetTexture("_Tex",sky);
                        RenderSettings.skybox = skyMaterial;
                    }
                }
                // load map.obj, the custom OBJLoader will handle things like ladders and tires
                bool mtlExists = System.IO.File.Exists(customMapPath+"\\map.mtl");
                GameObject Map = new OBJLoader().Load(customMapPath+"\\map.obj",mtlExists ? customMapPath+"\\map.mtl" : null);
                GameObject.Destroy(GameObject.Find("RoundTimer"));
            }
        }

        public static Plugin instance;
        public override void Load()
        {
            if (Il2CppSystem.Environment.GetEnvironmentVariable("MAPMOD_DISABLED") == "1"){
                var harmonyPatcher = new Harmony("disabled mapmod harmony");
                // in the very rare case of multiple sandboxed instances of the game running,
                // it may be desired to have all instances running from the same game folder
                // but only enable mapmod on some of them.
                // that is what this is for 
                // (bepinex detection needs to be disabled for that instance to run properly
                // so the mod still loads but only to patch out the detection)
                harmonyPatcher.PatchAll(typeof(bepinexDetectionPatch));
                return;
            }
            instance = this;
            LoadConfig();
            ClassInjector.RegisterTypeInIl2Cpp<Spinner>();
            ClassInjector.RegisterTypeInIl2Cpp<Checkpoint>();
            registerLoaderAction(defaultLoaderActions);
            var harmony = new Harmony("MapMod");
            harmony.PatchAll();
            harmony.PatchAll(typeof(bepinexDetectionPatch));
            SceneManager.sceneLoaded+=(UnityAction<Scene,LoadSceneMode>)onSceneLoad;
            Log.LogInfo("MapMod is loaded!");
        }
        public static ConfigEntry<bool> deselectAllMaps;
        public static ConfigEntry<bool> selectCustomMaps;
        public static ConfigEntry<string> discordLink;
        public static ConfigEntry<bool> sendInstallInstructions;
        public static void LoadConfig(){
            deselectAllMaps = instance.Config.Bind<bool>("Host Options","deselectVanilla",true,"deselect vanilla maps in the lobby creation menu by default.");
            selectCustomMaps = instance.Config.Bind<bool>("Host Options","selectCustom",true,"select custom maps automatically in the lobby creation menu.");
            discordLink = instance.Config.Bind<string>("Host Options","discordLink","discord.gg/NEfJW2Cff3","when vanilla players join and are unable to load into custom maps, they are send this discord link for info and help with installing the mod. by default this points to the #how-to-install-mapmod channel in the modding discord but you can set it to whatever you want.");
            sendInstallInstructions = instance.Config.Bind<bool>("Host Options","sendInstallInstructions",true,"if false, completely disable the installation instructions getting sent to vanilla players.");
        }

        [HarmonyPatch(typeof(SteamworksNative.SteamMatchmaking))]
        [HarmonyPatch(nameof(SteamworksNative.SteamMatchmaking.SetLobbyData))]
        static class SetLobbyDataPatch {
            public static void Prefix(SteamworksNative.CSteamID steamIDLobby,string pchKey,string pchValue){
                // add in our own lobby data whenever the version gets set (which happens once when creating a lobby)
                if (pchKey == "Version"){
                    SteamworksNative.SteamMatchmaking.SetLobbyData(steamIDLobby, "indexUrl", hostIndex);

                    if (!dormMap)
                        dormMap = mapManager.defaultMap;

                    if (mapManager.defaultMap.mapName != dormMap.mapName)
                        mapManager.defaultMap = dormMap;

                    foreach(Map customMap in mapManager.playableMaps)
                    {
                        string mapfolder = currentIndexFolderPath + customMap.name;

                        if (!System.IO.File.Exists(mapfolder+"\\map.config"))
                            continue;

                        string config = System.IO.File.ReadAllText(mapfolder+"\\map.config");

                        if (config.Contains("lobbymap"))
                            mapManager.defaultMap = customMap;
                    }
                }
            }
        }
        [HarmonyPatch(typeof(MonoBehaviourPublicObInUIgaStCSBoStcuCSUnique),"Method_Private_Void_LobbyEnter_t_PDM_1")]
        public static class onJoinPatch{
            public static void Postfix(MonoBehaviourPublicObInUIgaStCSBoStcuCSUnique __instance, SteamworksNative.LobbyEnter_t param_1){
                if (__instance.IsLobbyOwner()) {
                    return;
                }
                string url = SteamworksNative.SteamMatchmaking.GetLobbyData((SteamworksNative.CSteamID)param_1.m_ulSteamIDLobby, "indexUrl");
                indexUrl = url;
                updateIndex().Wait();
                registerMaps();
            }
        }
        [HarmonyPatch(typeof(MonoBehaviourPublicGamaTrmaUnique), "Method_Private_Void_0")]
        class MapUiHook
        {
            public static void setupUI(ref MonoBehaviourPublicGamaTrmaUnique __instance, string mapsfolder) {
                foreach (string path in System.IO.Directory.EnumerateDirectories(mapsfolder))
                {
                    bool mapExists = System.IO.File.Exists(path + "\\map.obj");
                    bool thumbnailExists = System.IO.File.Exists(path + "\\map.png");
                    if (mapExists)
                    {
                        GameObject btn = new GameObject();
                        btn.AddComponent<RectTransform>();
                        btn.AddComponent<UnityEngine.UI.Button>();
                        btn.AddComponent<CanvasRenderer>();
                        UnityEngine.UI.RawImage img = btn.AddComponent<UnityEngine.UI.RawImage>();
                        if (thumbnailExists)
                        {
                            byte[] imageData = System.IO.File.ReadAllBytes(path + "\\map.png");
                            Texture2D thumbnail = new Texture2D(1, 1);
                            UnityEngine.ImageConversion.LoadImage(thumbnail, imageData);
                            img.texture = thumbnail;
                        }
                        btn.GetComponent<UnityEngine.UI.Button>().onClick.AddListener((UnityAction)(() =>
                        {
                            // just load the most empty scene that still has the player in it
                            SceneManager.LoadScene("Skybox");
                            Plugin.loadingCustomMap = true;
                            Plugin.customMapPath = path;
                        }));
                        btn.transform.SetParent(__instance.mapContainer.transform, false);
                    }
                } 
            }
            [HarmonyPostfix]
            public static void practiceMenuHook(ref MonoBehaviourPublicGamaTrmaUnique __instance)
            {
                string datadir = Application.dataPath;
                string mapsfolder = System.IO.Directory.GetParent(datadir)+"\\TestMaps";
                if (System.IO.Directory.Exists(mapsfolder))
                {
                    setupUI(ref __instance, mapsfolder);
                } else {
                    System.IO.Directory.CreateDirectory(mapsfolder);
                    setupUI(ref __instance, mapsfolder);
                }
            }
        }
        [HarmonyPatch(typeof(MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique),"Start")]
        public static class playerStartHook {
            [HarmonyPostfix]
            public static void PostFix(MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique __instance){
                MonoBehaviourPublicCSstReshTrheObplBojuUnique steamData = __instance.GetComponent<MonoBehaviourPublicCSstReshTrheObplBojuUnique>();
                if (SceneManager.GetActiveScene().name == "Skybox" && steamManager.IsLobbyOwner() && steamData.steamProfile == steamManager.field_Private_CSteamID_0){
                    MonoBehaviourPublicObInGaspUnique spawnZoneManager = GameObject.Find("/SpawnZoneManager").GetComponent<MonoBehaviourPublicObInGaspUnique>();
                    __instance.transform.position = spawnZoneManager.FindGroundedSpawnPosition(1);
                }
                if (SceneManager.GetActiveScene().name == "Skybox"){
                    // delete this bit of the scoreboard object
                    GameObject obj = GameObject.Find("/Cube");
                    if (obj != null) {
                        GameObject.Destroy(obj);
                    }
                }
            } 
        }
        public static bool defaultLoaderActions(GameObject go, Mesh msh) {
            MeshFilter mf = go.GetComponent<MeshFilter>();
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (go.name.Contains("pixel")) {
                mr.material.mainTexture.filterMode = FilterMode.Point;
            }
            if (go.name.Contains("spawnzone"))
            {
                MonoBehaviourPublicVesiUnique zone = GameObject.Find("/SpawnZoneManager").transform.GetChild(0).gameObject.GetComponent<MonoBehaviourPublicVesiUnique>();
                Vector3 pos = mr.bounds.center;
                pos.x *= -1;
                zone.gameObject.transform.position = pos;
                zone.size = mr.bounds.size;
                GameObject.Destroy(go);
                return false;
            }
            if (!go.name.Contains("nocol"))
            {
                if (go.name.Contains("ice1"))
                    go.tag = "IceButNotThatIcy";
                if (go.name.Contains("ice2"))
                    go.tag = "Ice";
                MeshCollider mcol = go.AddComponent<MeshCollider>();
                mcol.sharedMesh = msh;
                go.layer = 6;
                if (go.name.Contains("ladder"))
                {
                    if (go.name.Contains("flip"))
                    {
                        go.transform.RotateAround(mcol.bounds.center, Vector3.up, 180);
                    }
                    if (go.name.Contains("rot90"))
                    {
                        go.transform.RotateAround(mcol.bounds.center, Vector3.up, 90);
                    }
                    go.layer = 14;
                    go.AddComponent<MonoBehaviourPublicLi1CoonUnique>();
                    mcol.convex = true;
                    mcol.isTrigger = true;
                }
                if (go.name.Contains("tire"))
                {
                    go.layer = 14;
                    mcol.convex = true;
                    mcol.isTrigger = true;
                    MonoBehaviourPublicSiBopuSiUnique tireScript = go.AddComponent<MonoBehaviourPublicSiBopuSiUnique>();
                    tireScript.field_Private_Boolean_0 = true;
                    tireScript.field_Private_Single_0 = 0.25f;
                    tireScript.pushForce = 35;
                    string forceValue = Plugin.tryGetValue(go.name, "tforce");
                    if (forceValue != null){
                        try {
                            tireScript.pushForce = int.Parse(forceValue);
                        } catch {}
                    }
                }
                if (go.name.Contains("boom"))
                {
                    MonoBehaviourPublicSicofoSimuupInSiboVeUnique script = go.AddComponent<MonoBehaviourPublicSicofoSimuupInSiboVeUnique>();
                    script.force = 40;
                    script.upForce = 15;
                    script.field_Private_Boolean_0 = true;
                    script.cooldown = 0.5f;
                    string forceValue = Plugin.tryGetValue(go.name, "bforce");
                    if (forceValue != null){
                        try {
                            script.force = int.Parse(forceValue);
                        } catch {}
                    }
                    string upForceValue = Plugin.tryGetValue(go.name, "upforce");
                    if (upForceValue != null){
                        try {
                            script.upForce = int.Parse(upForceValue);
                        } catch {}
                    }
                }
                if (go.name.Contains("spinner"))
                {
                    Rigidbody rb = go.AddComponent<Rigidbody>();
                    rb.isKinematic = true;
                    Spinner spinner = go.AddComponent<Spinner>();
                    string speedValue = Plugin.tryGetValue(go.name, "rspeed");
                    if (speedValue != null) {
                        try {
                            spinner.speed = float.Parse(speedValue);
                        } catch {}
                    }
                    string axisValue = Plugin.tryGetValue(go.name, "raxis");
                    if (axisValue != null) {
                        Vector3 axisVector = parseVector(axisValue);
                        spinner.axis = axisVector.normalized;
                    }
                }
                if (go.name.Contains("checkpoint")) go.AddComponent<Checkpoint>();
                if (go.name.Contains("snowballpile")){
                    // the game checks distance to the object's origin but since the object is at 0,0,0 and only the vertices are offset, 
                    // this bit of code centers the mesh back to 0,0,0 and then sets the transform to offset instead
                    Vector3 offsetFromOrigin = mr.bounds.center;
                    Vector3[] vertices = mf.sharedMesh.vertices;
                    for (int i = 0; i < vertices.Length; i++){
                        vertices[i] -= offsetFromOrigin;
                    }
                    mf.sharedMesh.vertices = vertices;
                    go.transform.position = offsetFromOrigin;
                    // make sure the bounds are updated so that culling uses the new position rather than the old one, causing the mesh to incorrectly go invisible sometimes
                    mf.sharedMesh.RecalculateBounds();
                    
                    MonoBehaviourPublicDi2InObInObInUnique sharedObjManager = GameObject.Find("/GameManager (1)/SharedObjectManager").GetComponent<MonoBehaviourPublicDi2InObInObInUnique>();
                    go.layer = 9; // "Interact" layer
                    MonoBehaviour1PublicBoInSiUnique snowballScript = go.AddComponent<MonoBehaviour1PublicBoInSiUnique>();
                    snowballScript.SetId(sharedObjManager.GetNextId());
                    sharedObjManager.AddObject(snowballScript);
                }
                if (go.name.Contains("lobbybutton")){
                    // the game checks distance to the object's origin but since the object is at 0,0,0 and only the vertices are offset, 
                    // this bit of code centers the mesh back to 0,0,0 and then sets the transform to offset instead
                    Vector3 offsetFromOrigin = mr.bounds.center;
                    Vector3[] vertices = mf.sharedMesh.vertices;
                    for (int i = 0; i < vertices.Length; i++){
                        vertices[i] -= offsetFromOrigin;
                    }
                    mf.sharedMesh.vertices = vertices;
                    go.transform.position = offsetFromOrigin;
                    // make sure the bounds are updated so that culling uses the new position rather than the old one, causing the mesh to incorrectly go invisible sometimes
                    mf.sharedMesh.RecalculateBounds();

                    MonoBehaviourPublicDi2InObInObInUnique sharedObjManager = GameObject.Find("/GameManager (1)/SharedObjectManager").GetComponent<MonoBehaviourPublicDi2InObInObInUnique>();
                    go.layer = 9; // "Interact" layer
                    MonoBehaviour1PublicTrbuObreunObBoVeVeVeUnique buttonScript = go.AddComponent<MonoBehaviour1PublicTrbuObreunObBoVeVeVeUnique>();
                    buttonScript.SetId(sharedObjManager.GetNextId());
                    sharedObjManager.AddObject(buttonScript);
                }
            }
            string rotValue = Plugin.tryGetValue(go.name, "rot");
            if (rotValue != null){
                try {
                    go.transform.RotateAround(mr.bounds.center, Vector3.up, int.Parse(rotValue));
                } catch {}
            }
            if (go.name.Contains("safezone"))
            {
                MonoBehaviourPublicLi1ObsaInObUnique script1 = go.AddComponent<MonoBehaviourPublicLi1ObsaInObUnique>();
                MonoBehaviourPublicVoCoOnVoCoVoCoVoCoVo1 script2 = go.AddComponent<MonoBehaviourPublicVoCoOnVoCoVoCoVoCoVo1>();
                go.GetComponent<MeshCollider>().convex = true;
                go.GetComponent<MeshCollider>().isTrigger = true;
                go.layer = 13;
            }
            if (go.name.Contains("invis"))
            {
                mr.enabled = false;
            }
            return true;
        }
        public static async Task updateIndex(){
            instance.Log.LogInfo("updating index: " + indexUrl);
            if (indexUrl == "useLocal") {
                currentIndexFolderPath = gameFolder + "\\Maps\\";
                customMaps = new System.Collections.Generic.List<Map>();
                foreach (string s in getCustomMapNames()){
                    customMaps.Add(createMap(s));
                }
                return;
            }
            System.Uri uri = new System.Uri(indexUrl);
            if (uri.Host != "github.com"){
                // only allow github repos
                return;
            }
            currentIndexFolderPath = gameFolder + "\\Maps\\" + indexUrl.Substring(indexUrl.LastIndexOf("github.com")).Replace("/","").Replace("\\","").Replace(".","")+"\\";
            if (!System.IO.Directory.Exists(currentIndexFolderPath)){
                System.IO.Directory.CreateDirectory(currentIndexFolderPath);
            }
            string filecontent;
            HttpClient client = new HttpClient();
            filecontent = await client.GetStringAsync(indexUrl+"indexfile");
            customMaps = new System.Collections.Generic.List<Map>();
            foreach (string line in filecontent.Split("\n")){
                int seperatorIndex = line.IndexOf("|");
                if (seperatorIndex < 0){
                    continue;
                }
                string version = line.Substring(0,seperatorIndex);
                string name = line.Substring(seperatorIndex+1).Trim();
                string mapfolderPath = currentIndexFolderPath + "\\" + name + "\\";
                if (!System.IO.Directory.Exists(mapfolderPath)){
                    System.IO.Directory.CreateDirectory(mapfolderPath);
                    await installMap(mapfolderPath,client,name,version);
                } else {
                    string installedVersion;
                    try {
                        installedVersion = System.IO.File.ReadAllText(mapfolderPath+"version");
                    } catch {
                        await installMap(mapfolderPath,client,name,version);
                        continue;
                    }
                    if (installedVersion != version){
                        await installMap(mapfolderPath,client,name,version);
                    }
                }
                customMaps.Add(createMap(name));
            }
        }
        public static async Task installMap(string mapfolderPath, HttpClient client, string name, string version){
            instance.Log.LogInfo("installing map: " + mapfolderPath + " " + name + " " +version);
            System.IO.Stream stream = await client.GetStreamAsync(indexUrl + "maps/" + name + ".zip");
            ZipArchive zip = new ZipArchive(stream);
            foreach (ZipArchiveEntry entry in zip.Entries){
                string path = System.IO.Path.GetFullPath(System.IO.Path.Join(mapfolderPath,entry.FullName));
                if (entry.FullName.EndsWith("/")) {
                    System.IO.Directory.CreateDirectory(path);
                } else {
                    entry.ExtractToFile(path, true);
                }
            }
            zip.Dispose();
            System.IO.File.WriteAllText(mapfolderPath+"version",version);
        }
    }
    [HarmonyPatch(typeof(SceneManager),nameof(SceneManager.LoadScene), new System.Type[]{typeof(string)})]
    class SceneLoadHook {
        public static void Prefix(ref string sceneName){
            for (int i = 62; i < Plugin.mapManager.maps.Length; i++){
                // if, for some reason, the game is trying to load a scene of a custom map instead of
                // skybox, catch it and load skybox instead (because there is no scene for custom maps).
                if (Plugin.mapManager.maps[i].name == sceneName){
                    sceneName = "Skybox";
                    // should trigger a custom map load
                    Plugin.mapManager.GetMap(i);
                    return;
                }
            }
        }
    }
    [HarmonyPatch(typeof(Debug),nameof(Debug.Log),new System.Type[] {typeof(Object)})]
    class DebugLogHook {
        public static bool Prefix(ref Object message){
            if (!Plugin.sendInstallInstructions.Value) return true;
            string s_message = message.ToString();
            if (s_message.Contains("is not ready to load, but is active") || s_message.Contains("tried to interact with the game but is eliminated")){
                Plugin.trySendInstallMessage();
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(MonoBehaviourPublicRaovTMinTemeColoonCoUnique), "AppendMessage")]
    class ChatBoxHook {
        public static bool Prefix(MonoBehaviourPublicRaovTMinTemeColoonCoUnique __instance, System.UInt64 param_1, string param_2, string param_3){
            if (param_1 == 1 && (param_2.Contains("this lobby has custom maps and it seems you dont have the mod installed") || param_2.Contains("join the discord for help with installation"))){
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(MonoBehaviourPublicTeprUIObUIBotiRabamaUnique),"Start")]
    class LoadingScreenPatch {
        public static void Postfix(MonoBehaviourPublicTeprUIObUIBotiRabamaUnique __instance){
            if (Plugin.loadingCustomMap){
                __instance.mapName.text = Plugin.customMapName;
            }
        }
    }

    [HarmonyPatch(typeof(MonoBehaviourPublicObInMamaLi1plMadeMaUnique), "GetMap")]
    class mapManagerHook { 
        [HarmonyPrefix]
        public static void prefix(int param_1, out int __state) {
            __state = param_1;
        }
        [HarmonyPostfix]
        public static void getMapHook(ref Map __result,MonoBehaviourPublicObInMamaLi1plMadeMaUnique __instance, int __state) {
            if (__state > 61) {
                string mapPath = Plugin.currentIndexFolderPath+__result.mapName;
                if (System.IO.Directory.Exists(mapPath)) {
                    Map skybox = __instance.GetMap(52);
                    skybox.mapThumbnail = __result.mapThumbnail;
                    //skybox.name = __result.mapName;
                    Plugin.customMapName = __result.mapName;
                    __result = skybox;
                    Plugin.loadingCustomMap = true;
                    Plugin.customMapPath = mapPath;
                    Plugin.lastCustomMapID = __state;
                } else {
                    __result = __instance.GetMap(14); // return the karlson map as a backup
                }
            }
        }
    }

    public class Spinner : MonoBehaviour {
        public Vector3 axis = new Vector3(0, 1, 0);
        public float speed = 3;

        public Spinner(System.IntPtr ptr) : base(ptr) { }

        void FixedUpdate() {
            Rigidbody rb = gameObject.GetComponent<Rigidbody>();
            Vector3 center = gameObject.GetComponent<MeshCollider>().bounds.center;
            Quaternion q = Quaternion.AxisAngle(axis,speed*0.03f);
            rb.MovePosition(q*(rb.transform.position-center)+center);
            rb.MoveRotation(q*rb.transform.rotation);
        }
    }
    public class Checkpoint : MonoBehaviour
    {
        void OnCollisionEnter(Collision col)
        {
            // if this collision is the player object
            if (col.gameObject.GetComponent<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>() != null)
            {
                // move the spawnzone to above this checkpoint
                GameObject spawnZone = GameObject.Find("/SpawnZoneManager").transform.GetChild(0).gameObject;
                Vector3 pos = GetComponent<MeshCollider>().bounds.center;
                pos.y += GetComponent<MeshCollider>().bounds.size.y;
                spawnZone.transform.position = pos;
            }
        }
    }
    // allow joining vanilla lobbies without disabling the mod
    class bepinexDetectionPatch {
        [HarmonyPatch(typeof(MonoBehaviourPublicGataInefObInUnique), "Method_Private_Void_GameObject_Boolean_Vector3_Quaternion_0")]
        [HarmonyPatch(typeof(MonoBehaviourPublicCSDi2UIInstObUIloDiUnique), "Method_Private_Void_0")]
        [HarmonyPatch(typeof(MonoBehaviourPublicVesnUnique), "Method_Private_Void_0")]
        [HarmonyPatch(typeof(MonoBehaviourPublicObjomaOblogaTMObseprUnique), "Method_Public_Void_PDM_2")]
        [HarmonyPatch(typeof(MonoBehaviourPublicTeplUnique), "Method_Private_Void_PDM_32")]
        [HarmonyPrefix]
        public static bool Prefix(System.Reflection.MethodBase __originalMethod)
        {
            return false;
        }
    }
}
