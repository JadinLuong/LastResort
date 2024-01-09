using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LastResort.Patches;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastResort
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class LastResortMod : BaseUnityPlugin
    {
        private const string modGUID = "LastResort";
        private const string modName = "LastResort";
        private const string modVersion = "1.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);

        private static LastResortMod Instance;

        public static ManualLogSource Log;

        public static ConfigEntry<Key> explodeKey;

        public static Key defaultKey = Key.P;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            Log = BepInEx.Logging.Logger.CreateLogSource(modGUID);

            explodeKey = Config.Bind("Binds", "Explode", defaultKey, "");

            Log.LogInfo("Binded " + explodeKey.Value + " key to explode. You can bind this to a different key by changing the settings in the mod's configuration file.");

            harmony.PatchAll(typeof(LastResortMod));
            harmony.PatchAll(typeof(PlayerControllerBPatch));
            harmony.PatchAll(typeof(GameNetworkManagerPatch));
            harmony.PatchAll(typeof(StartOfRoundPatch));

            Log.LogInfo("Ready to Explode!");

            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }
    }
}
