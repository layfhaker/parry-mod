using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using RepoValuableParry.Core;
using RepoValuableParry.DebugTools;
using UnityEngine;

namespace RepoValuableParry
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static Plugin Instance { get; private set; }
        static ManualLogSource _log;
        Harmony _harmony;

        void Awake()
        {
            Instance = this;
            _log = Logger;
            ParryConfig.Bind(Config);

            gameObject.transform.parent = null;
            gameObject.hideFlags = HideFlags.HideAndDontSave;

            ParryManager.Ensure();
            var mgr = ParryManager.Instance.gameObject;
            if (mgr.GetComponent<ParryDebugOverlay>() == null)
                mgr.AddComponent<ParryDebugOverlay>();
            if (mgr.GetComponent<DebugGizmos>() == null)
                mgr.AddComponent<DebugGizmos>();

            _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            _harmony.PatchAll();

            Log($"{PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION} loaded.");
        }

        void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }

        internal static void Log(string message) => _log?.LogInfo(message);
        internal static void LogWarning(string message) => _log?.LogWarning(message);
        internal static void LogError(string message) => _log?.LogError(message);

        internal static void LogVerbose(string message)
        {
            if (ParryConfig.VerboseLogs != null && ParryConfig.VerboseLogs.Value)
                _log?.LogInfo("[VERBOSE] " + message);
        }
    }
}
