using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using System.Reflection;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastResort.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
    {
        public static ManualLogSource lastResortLog = LastResortMod.Log;

        public static PlayerControllerB player;

        public static bool explodeTipShown;

        public static bool playerExploded;


        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void initialize(PlayerControllerB __instance)
        {
            if (NetworkManager.Singleton.LocalClientId == __instance.playerClientId)
            {
                lastResortLog.LogInfo("Initializing");

                player = __instance;

                LastResortHandler.Instance.LastResortAudioSource = player.itemAudio;
                LastResortHandler.Instance.LastResortAudioClip = Resources.FindObjectsOfTypeAll<Landmine>()[0].mineTrigger;

                explodeTipShown = false;

                playerExploded = false;
            }
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static async void PlayerControllerB_Update(PlayerControllerB __instance)
        {
            if (NetworkManager.Singleton.LocalClientId == __instance.playerClientId)
            {
                bool keyPressed = Keyboard.current[LastResortMod.explodeKey.Value].wasPressedThisFrame;

                if (StartOfRound.Instance.shipHasLanded && !explodeTipShown)
                {
                    lastResortLog.LogInfo("Explode tip has been shown");
                    HUDManager.Instance.DisplayTip("Last Resort Mod", "You can now explode yourself by pressing the " + LastResortMod.explodeKey.Value + " key.");
                    explodeTipShown = true;
                }
                else if (!StartOfRound.Instance.shipHasLanded && explodeTipShown)
                {
                    lastResortLog.LogInfo("Explode tip has been set to false");
                    explodeTipShown = false;
                    playerExploded = false;
                }
                if (keyPressed && !playerExploded && explodeTipShown && !player.isPlayerDead)
                {
                    playerExploded = true;
                    LastResortHandler.Instance.PlayExplosionSoundServerRpc();
                    await Task.Delay(500);
                    LastResortHandler.Instance.ExplodePositionServerRpc(player.transform.position);
                }
            }
        }
    }

    public class LastResortHandler : NetworkBehaviour
    {
        public static LastResortHandler Instance;

        public AudioSource LastResortAudioSource;

        public AudioClip LastResortAudioClip;

        public override void OnNetworkSpawn()
        {
            Instance = this;

            base.OnNetworkSpawn();
        }

        [ServerRpc(RequireOwnership = false)]
        public void ExplodePositionServerRpc(Vector3 position)
        {
            ExplodePositionClientRpc(position);
        }

        [ClientRpc]
        public void ExplodePositionClientRpc(Vector3 position)
        {
            Landmine.SpawnExplosion(position, true, 5.7f, 5.7f);
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlayExplosionSoundServerRpc()
        {
            PlayExplosionSoundClientRpc();
        }

        [ClientRpc]
        public void PlayExplosionSoundClientRpc()
        {
            LastResortAudioSource.PlayOneShot(LastResortAudioClip);
        }
    }

    [HarmonyPatch(typeof(GameNetworkManager))]
    internal class GameNetworkManagerPatch
    {

        public static AssetBundle LastResortAssetBundle;

        public static GameObject LastResortNetworkPrefab;

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void Init()
        {
            LastResortMod.Log.LogInfo("GameNetworkManager Start");

            LastResortAssetBundle = AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("LastResort.lastresortasset"));
            LastResortNetworkPrefab = LastResortAssetBundle.LoadAsset<GameObject>("assets/lastresort/lastresortnetwork.prefab");
            LastResortNetworkPrefab.AddComponent<LastResortHandler>();

            NetworkManager.Singleton.AddNetworkPrefab(LastResortNetworkPrefab);
        }
    }

    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void SpawnNetworkHandler()
        {
            LastResortMod.Log.LogInfo("StartOfRound Start");
            if(NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                var networkHandlerHost = Object.Instantiate(GameNetworkManagerPatch.LastResortNetworkPrefab, Vector3.zero, Quaternion.identity);
                networkHandlerHost.GetComponent<NetworkObject>().Spawn();
            }
        }
    }
}
