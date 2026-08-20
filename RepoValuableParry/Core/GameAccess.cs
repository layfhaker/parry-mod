using HarmonyLib;
using UnityEngine;

namespace RepoValuableParry.Core
{
    /// <summary>
    /// Cached access to internal game fields that the public API does not expose.
    /// Prefer public SemiFunc / GetComponent paths whenever they exist.
    /// </summary>
    internal static class GameAccess
    {
        static readonly AccessTools.FieldRef<ValuableObject, float> DollarValueCurrent =
            AccessTools.FieldRefAccess<ValuableObject, float>("dollarValueCurrent");

        static readonly AccessTools.FieldRef<PlayerAvatar, PlayerTumble> Tumble =
            AccessTools.FieldRefAccess<PlayerAvatar, PlayerTumble>("tumble");

        static readonly AccessTools.FieldRef<PlayerAvatar, bool> IsLocal =
            AccessTools.FieldRefAccess<PlayerAvatar, bool>("isLocal");

        static readonly AccessTools.FieldRef<PhysGrabber, PhysGrabObject> GrabbedPhys =
            AccessTools.FieldRefAccess<PhysGrabber, PhysGrabObject>("grabbedPhysGrabObject");

        static readonly AccessTools.FieldRef<Enemy, EnemyRigidbody> EnemyRb =
            AccessTools.FieldRefAccess<Enemy, EnemyRigidbody>("Rigidbody");

        static readonly AccessTools.FieldRef<Enemy, bool> HasRb =
            AccessTools.FieldRefAccess<Enemy, bool>("HasRigidbody");

        static readonly AccessTools.FieldRef<Enemy, EnemyHealth> Health =
            AccessTools.FieldRefAccess<Enemy, EnemyHealth>("Health");

        static readonly AccessTools.FieldRef<Enemy, bool> HasHealth =
            AccessTools.FieldRefAccess<Enemy, bool>("HasHealth");

        static readonly AccessTools.FieldRef<Enemy, EnemyStateStunned> Stunned =
            AccessTools.FieldRefAccess<Enemy, EnemyStateStunned>("StateStunned");

        static readonly AccessTools.FieldRef<Enemy, bool> HasStunned =
            AccessTools.FieldRefAccess<Enemy, bool>("HasStateStunned");

        static readonly AccessTools.FieldRef<EnemyRigidbody, Rigidbody> ErbRb =
            AccessTools.FieldRefAccess<EnemyRigidbody, Rigidbody>("rb");

        static readonly AccessTools.FieldRef<PhysGrabObjectImpactDetector, ValuableObject> DetectorValuable =
            AccessTools.FieldRefAccess<PhysGrabObjectImpactDetector, ValuableObject>("valuableObject");

        static readonly AccessTools.FieldRef<PlayerHealth, int> HealthCurrent =
            AccessTools.FieldRefAccess<PlayerHealth, int>("health");

        public static float GetDollarValue(ValuableObject valuable)
        {
            if (valuable == null)
                return 0f;
            try { return DollarValueCurrent(valuable); }
            catch { return 0f; }
        }

        public static PlayerTumble GetTumble(PlayerAvatar player)
        {
            if (player == null)
                return null;
            try
            {
                var tumble = Tumble(player);
                if (tumble != null)
                    return tumble;
            }
            catch
            {
                // fall through
            }
            return player.GetComponentInChildren<PlayerTumble>();
        }

        public static bool IsLocalPlayer(PlayerAvatar player)
        {
            if (player == null)
                return false;
            var local = SemiFunc.PlayerGetLocal();
            if (local != null)
                return player == local;
            try { return IsLocal(player); }
            catch { return false; }
        }

        public static PhysGrabObject GetGrabbed(PhysGrabber grabber)
        {
            if (grabber == null)
                return null;
            var viaSemi = SemiFunc.PhysGrabberGetGrabbedPhysGrabObject(grabber);
            if (viaSemi != null)
                return viaSemi;
            try { return GrabbedPhys(grabber); }
            catch { return null; }
        }

        public static bool EnemyHasRigidbody(Enemy enemy)
        {
            if (enemy == null)
                return false;
            try { return HasRb(enemy) && EnemyRb(enemy) != null; }
            catch { return enemy.GetComponentInChildren<EnemyRigidbody>() != null; }
        }

        public static EnemyRigidbody GetEnemyRigidbody(Enemy enemy)
        {
            if (enemy == null)
                return null;
            try
            {
                var rb = EnemyRb(enemy);
                if (rb != null)
                    return rb;
            }
            catch
            {
                // fall through
            }
            return enemy.GetComponentInChildren<EnemyRigidbody>();
        }

        public static Rigidbody GetRigidbody(EnemyRigidbody enemyRb)
        {
            if (enemyRb == null)
                return null;
            try
            {
                var rb = ErbRb(enemyRb);
                if (rb != null)
                    return rb;
            }
            catch
            {
                // fall through
            }
            return enemyRb.GetComponent<Rigidbody>();
        }

        public static EnemyHealth GetHealth(Enemy enemy)
        {
            if (enemy == null)
                return null;
            try
            {
                if (!HasHealth(enemy))
                    return null;
                return Health(enemy);
            }
            catch
            {
                return enemy.GetComponent<EnemyHealth>();
            }
        }

        public static void TryStun(Enemy enemy, float duration)
        {
            if (enemy == null)
                return;
            try
            {
                if (!HasStunned(enemy))
                    return;
                var state = Stunned(enemy);
                if (state == null)
                    return;
                var set = AccessTools.Method(typeof(EnemyStateStunned), "Set", new[] { typeof(float) });
                set?.Invoke(state, new object[] { duration });
            }
            catch
            {
                // stunning is optional flavour; freeze is the real capture
            }
        }

        public static int GetPlayerHealth(PlayerAvatar player)
        {
            if (player == null || player.playerHealth == null)
                return 0;
            try { return HealthCurrent(player.playerHealth); }
            catch { return 1; }
        }

        public static ValuableObject GetValuableFromDetector(PhysGrabObjectImpactDetector detector)
        {
            if (detector == null)
                return null;
            try { return DetectorValuable(detector); }
            catch { return detector.GetComponent<ValuableObject>(); }
        }
    }
}
