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
    [BepInPlugin("CrabGameMapMod", "MapMod", "0.5")]
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
        public static Vector3 parseVector(string text) {
            string[] array = text.Split(",");
            if (array.Length == 3) {
                return new Vector3(float.Parse(array[0]), float.Parse(array[1]), float.Parse(array[2]));
            } else { return new Vector3(0,1,0); }
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
        public static System.Collections.Generic.List<System.Func<GameObject,Mesh,bool>> mapLoaderActions = new System.Collections.Generic.List<System.Func<GameObject, Mesh, bool>>();
        public static void registerLoaderAction(System.Func<GameObject,Mesh,bool> action) {
            mapLoaderActions.Add(action);
        }
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
            registerLoaderAction(defaultLoaderActions);
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
                    if (forceValue != null)
                        tireScript.pushForce = int.Parse(forceValue);
                }
                if (go.name.Contains("boom"))
                {
                    MonoBehaviourPublicSicofoSimuupInSiboVeUnique script = go.AddComponent<MonoBehaviourPublicSicofoSimuupInSiboVeUnique>();
                    script.force = 40;
                    script.upForce = 15;
                    script.field_Private_Boolean_0 = true;
                    script.cooldown = 0.5f;
                    string forceValue = Plugin.tryGetValue(go.name, "bforce");
                    if (forceValue != null)
                        script.force = int.Parse(forceValue);
                    string upForceValue = Plugin.tryGetValue(go.name, "upforce");
                    if (upForceValue != null)
                        script.upForce = int.Parse(upForceValue);
                }
                if (go.name.Contains("spinner"))
                {
                    Rigidbody rb = go.AddComponent<Rigidbody>();
                    //rb.centerOfMass = mcol.bounds.center;
                    rb.isKinematic = true;
                    Spinner spinner = go.AddComponent<Spinner>();
                    string speedValue = Plugin.tryGetValue(go.name, "rspeed");
                    if (speedValue != null)
                        spinner.speed = float.Parse(speedValue);
                    string axisValue = Plugin.tryGetValue(go.name, "raxis");
                    if (axisValue != null) {
                        Vector3 axisVector = parseVector(axisValue);
                        spinner.axis = axisVector.normalized;
                    }
                }
                if (go.name.Contains("checkpoint"))
                    go.AddComponent<Checkpoint>();
            }
            string rotValue = Plugin.tryGetValue(go.name, "rot");
            if (rotValue != null)
                go.transform.RotateAround(mr.bounds.center, Vector3.up, int.Parse(rotValue));
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
}
