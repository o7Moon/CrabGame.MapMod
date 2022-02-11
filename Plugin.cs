using BepInEx;
using BepInEx.IL2CPP;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Dummiesman;
using UnhollowerRuntimeLib;
using System.Linq;

namespace MapMod
{
    [BepInPlugin("CrabGameMapMod", "MapMod", "0.4")]
    public class Plugin : BasePlugin
    {
        // values in object names are formatted valueName[value]
        public static string tryGetValue(string name,string valueName) {
            if (!name.Contains(valueName))
                return null;
            try
            {
                string valueNameWhole = valueName + "[";
                int valueIndex = name.IndexOf(valueNameWhole) + valueName.Length + 1;
                int valueEndIndex = name.IndexOf("]", valueIndex);
                return name.Substring(valueIndex, valueEndIndex - valueIndex);
            } catch {
                return null;
            }
        }
        public static string[] getCustomMapNames() {
            string path = System.IO.Directory.GetParent(Application.dataPath) + "\\Maps.txt";
            if (!System.IO.File.Exists(path)) {
                System.IO.File.WriteAllText(path," ");
                return null;
            }
            return System.IO.File.ReadAllLines(path);
        }
        public static void registerMaps() {
            string[] mapList = getCustomMapNames();
            string mapsFolder = System.IO.Directory.GetParent(Application.dataPath) + "\\Maps";
            if (!System.IO.Directory.Exists(mapsFolder)) {
                System.IO.Directory.CreateDirectory(mapsFolder);
            }
            GameObject mapManager = GameObject.Find("/Managers/Map&GameModes");
            MonoBehaviourPublicObInMamaLi1plMadeMaUnique mapScript = mapManager.GetComponent<MonoBehaviourPublicObInMamaLi1plMadeMaUnique>();
            MonoBehaviourPublicGadealGaLi1pralObInUnique gamemodeScript = mapManager.GetComponent<MonoBehaviourPublicGadealGaLi1pralObInUnique>();

            string[] gamemodes = new string[gamemodeScript.allGameModes.Length];
            for (int i = 0; i < gamemodeScript.allGameModes.Length; i++) {
                gamemodes[i] = gamemodeScript.allGameModes[i].modeName.ToLower();
            }
            int mapNum = 62;
            foreach (string name in mapList) {
                Map map = ScriptableObject.CreateInstance<Map>();
                map.name = name;
                map.mapName = name;
                map.id = mapNum;
                mapNum++;
                string mapFolder = mapsFolder + "\\" + name;
                Texture2D thumbnail = new Texture2D(1, 1);
                if (System.IO.File.Exists(mapFolder + "\\map.png")) {
                    ImageConversion.LoadImage(thumbnail,System.IO.File.ReadAllBytes(mapFolder+"\\map.png"));
                }
                map.mapThumbnail = thumbnail;
                if (System.IO.File.Exists(mapFolder + "\\map.config")) {
                    string config = System.IO.File.ReadAllText(mapFolder+"\\map.config");
                    string mapSize = tryGetValue(config,"size");
                    if (mapSize != null) { 
                        switch (mapSize) { 
                            case "large":
                                map.mapSize = Map.EnumNPublicSealedvasmmelaan5vUnique.large;
                                break;
                            case "medium":
                                map.mapSize = Map.EnumNPublicSealedvasmmelaan5vUnique.medium;
                                break;
                            case "small":
                                map.mapSize = Map.EnumNPublicSealedvasmmelaan5vUnique.small;
                                break;
                            default:
                                map.mapSize = Map.EnumNPublicSealedvasmmelaan5vUnique.any;
                                break;
                        }
                    } else {
                        map.mapSize = Map.EnumNPublicSealedvasmmelaan5vUnique.any;
                    }
                    string mapModes = tryGetValue(config,"modes");
                    if (mapModes != null)
                    {
                        for (int i = 0; i < gamemodes.Length; i++)
                        {
                            string mode = gamemodes[i];
                            if (mapModes.Contains(mode)) {
                                gamemodeScript.allGameModes[i].compatibleMaps = gamemodeScript.allGameModes[i].compatibleMaps.AddItem(map).ToArray();
                            }
                        }
                    }
                }
                mapScript.maps = mapScript.maps.AddItem(map).ToArray();
                mapScript.playableMaps = new Il2CppSystem.Collections.Generic.List<Map>();
            }
        }
        public static bool loadingCustomMap = false;
        public static bool allObjectsTextured = false;
        public static bool gameLoaded = false;
        public static string customMapPath;
        public static void onSceneLoad(Scene scene, LoadSceneMode mode) { 
            if (!gameLoaded) {
                gameLoaded = true;
                registerMaps();
            }
            // once we load a scene, if this load was caused by pressing a custom map button then delete the default map and add our own
            if (loadingCustomMap && scene.name != "LoadingScreen") {
                loadingCustomMap = false;
                // make spawning consistent
                GameObject.Find("/SpawnZoneManager").transform.GetChild(0).GetComponent<MonoBehaviourPublicVesiUnique>().size = new Vector3(2,2,2);
                GameObject.Destroy(GameObject.Find("/Map"));
                string mapConfigPath = customMapPath + "\\map.config";
                if (System.IO.File.Exists(mapConfigPath))
                {
                    string config = System.IO.File.ReadAllText(mapConfigPath);
                    allObjectsTextured = false;
                    if (config.Contains("allObjectsTextured"))
                        allObjectsTextured = true;
                }
                // load map.obj, the custom OBJLoader will handle things like ladders and tires
                bool mtlExists = System.IO.File.Exists(customMapPath+"\\map.mtl");
                GameObject Map = new OBJLoader().Load(customMapPath+"\\map.obj",mtlExists ? customMapPath+"\\map.mtl" : null);
            }
        }
        public override void Load()
        {
            ClassInjector.RegisterTypeInIl2Cpp<Spinner>();
            ClassInjector.RegisterTypeInIl2Cpp<Checkpoint>();
            var harmony = new Harmony("MapMod");
            harmony.PatchAll();
            SceneManager.sceneLoaded+=(UnityAction<Scene,LoadSceneMode>)onSceneLoad;
            Log.LogInfo("MapMod is loaded!");
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
                string mapPath = System.IO.Directory.GetParent(Application.dataPath) + "\\Maps\\"+__result.mapName;
                if (System.IO.Directory.Exists(mapPath)) {
                    __result = __instance.GetMap(52);
                    Plugin.loadingCustomMap = true;
                    Plugin.customMapPath = mapPath;
                } else {
                    __result = __instance.GetMap(14); // return the karlson map as a backup
                }
            }
        }
    }

    public class Spinner : MonoBehaviour {
        public Vector3 axis = new Vector3(0, 1, 0);
        public int speed = 3;

        public Spinner(System.IntPtr ptr) : base(ptr) { }

        void FixedUpdate() {
            gameObject.transform.RotateAround(gameObject.GetComponent<Collider>().bounds.center, axis, speed);
        }
    }
    public class Checkpoint : MonoBehaviour {
        void OnCollisionEnter(Collision col) { 
            // if this collision is the player object
            if (col.gameObject.GetComponent<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>() != null) {
                // move the spawnzone to above this checkpoint
                GameObject spawnZone = GameObject.Find("/SpawnZoneManager").transform.GetChild(0).gameObject;
                Vector3 pos = GetComponent<MeshCollider>().bounds.center;
                pos.y += GetComponent<MeshCollider>().bounds.size.y;
                spawnZone.transform.position = pos;
            }
        }
    }
}
