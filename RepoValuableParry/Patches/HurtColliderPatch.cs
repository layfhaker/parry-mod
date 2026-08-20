using HarmonyLib;
using RepoValuableParry.Core;
using UnityEngine;

namespace RepoValuableParry.Patches
{
    [HarmonyPatch(typeof(HurtCollider), "PlayerHurt")]
    static class HurtColliderPatch
    {
        static bool Prefix(HurtCollider __instance, PlayerAvatar _player)
        {
            if (!ParryConfig.Enabled.Value)
                return true;
            if (ParryManager.Instance == null)
                return true;
            if (_player == null)
                return true;

            if (ParryManager.Instance.IsAttackConsumed(__instance))
            {
                Plugin.LogVerbose("Skipping consumed HurtCollider hit.");
                return false;
            }

            if (ParryManager.Instance.TryInterceptAttack(__instance, _player))
                return false;

            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerAvatar), "Awake")]
    static class PlayerAvatarAwakePatch
    {
        static void Postfix(PlayerAvatar __instance)
        {
            Networking.ParryNetworkManager.EnsureOnPlayer(__instance);
        }
    }
}
