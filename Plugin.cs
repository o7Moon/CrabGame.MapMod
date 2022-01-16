using BepInEx;
using BepInEx.IL2CPP;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Dummiesman;

namespace MapMod
{
    [BepInPlugin("CrabGameMapMod", "MapMod", "0.1")]
    public class Plugin : BasePlugin
    {
        public static bool loadingCustomMap = false;
        public static string customMapPath;
        public static void onSceneLoad(Scene scene, LoadSceneMode mode) { 
            // once we load a scene, if this load was caused by pressing a custom map button then delete the default map and add our own
            if (loadingCustomMap) {
                loadingCustomMap = false;
                // make spawning consistent
                GameObject.Find("/SpawnZoneManager").transform.GetChild(0).GetComponent<MonoBehaviourPublicVesiUnique>().size = new Vector3(2,2,2);
                GameObject.Destroy(GameObject.Find("/Map"));
                // load map.obj, the custom OBJLoader will handle things like ladders and tires
                GameObject Map = new OBJLoader().Load(System.IO.File.Open(customMapPath+"\\map.obj",System.IO.FileMode.Open));
                Map.transform.Translate(new Vector3(0, -50, 0));
            }
        }
        public override void Load()
        {
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
                            // just load the most empty scene tthat still has the player in it
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
                string mapsfolder = System.IO.Directory.GetParent(datadir)+"\\Maps";
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
}
