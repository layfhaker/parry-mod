using HarmonyLib;
using UnityEngine;

namespace RepoValuableParry.Input
{
    internal static class ParryInput
    {
        static readonly AccessTools.FieldRef<ChatManager, bool> ChatActive =
            AccessTools.FieldRefAccess<ChatManager, bool>("chatActive");

        public static bool WasParryPressed()
        {
            if (IsChatOpen())
                return false;

            if (ParryConfig.UseVanillaInteract.Value)
            {
                if (InputManager.instance == null)
                    return UnityEngine.Input.GetKeyDown(KeyCode.E);
                return SemiFunc.InputDown(InputKey.Interact);
            }

            return UnityEngine.Input.GetKeyDown(ParryConfig.FallbackParryKey.Value);
        }

        static bool IsChatOpen()
        {
            if (ChatManager.instance == null)
                return false;
            try { return ChatActive(ChatManager.instance); }
            catch { return false; }
        }
    }
}
