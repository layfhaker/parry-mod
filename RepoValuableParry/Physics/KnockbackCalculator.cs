using UnityEngine;

namespace RepoValuableParry.Physics
{
    internal static class KnockbackCalculator
    {
        const float UpBias = 0.35f;

        public static Vector3 DirectionFrom(Vector3 from, Vector3 to)
        {
            Vector3 dir = to - from;
            dir.y += UpBias;
            if (dir.sqrMagnitude < 0.0001f)
                dir = Vector3.up + Vector3.back;
            return dir.normalized;
        }

        public static Vector3 PlayerForce(Vector3 detonationPoint, Vector3 playerPosition, float explosionForce)
        {
            return DirectionFrom(detonationPoint, playerPosition) *
                   explosionForce *
                   ParryConfig.PlayerKnockbackMultiplier.Value;
        }

        public static Vector3 EnemyForce(Vector3 detonationPoint, Vector3 enemyPosition, float explosionForce)
        {
            return DirectionFrom(detonationPoint, enemyPosition) *
                   explosionForce *
                   ParryConfig.EnemyKnockbackMultiplier.Value;
        }
    }
}
