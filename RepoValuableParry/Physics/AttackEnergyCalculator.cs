using UnityEngine;

namespace RepoValuableParry.Physics
{
    internal static class AttackEnergyCalculator
    {
        public static float Compute(HurtCollider collider)
        {
            if (collider == null)
                return 0f;

            GetRaw(collider, out float damage, out float force, out float tumble);

            // Force/tumble on lasers and charges are huge ragdoll numbers.
            // sqrt keeps them in the same ballpark as ParryCapacity (tens–low hundreds)
            // so a 3D printer can eat a clown laser, but a mug still cannot.
            return
                damage * ParryConfig.DamageWeight.Value +
                Mathf.Sqrt(force) * ParryConfig.ForceWeight.Value +
                Mathf.Sqrt(tumble) * ParryConfig.TumbleWeight.Value;
        }

        public static void GetRaw(HurtCollider collider, out float damage, out float force, out float tumble)
        {
            damage = collider != null ? collider.playerDamage : 0f;
            force = collider != null ? Mathf.Max(0f, Mathf.Max(collider.physHitForce, collider.playerHitForce)) : 0f;
            tumble = collider != null ? Mathf.Max(0f, collider.playerTumbleForce) : 0f;
        }

        public static void ScaleExplosion(float energy, out float force, out float radius)
        {
            float minE = ParryConfig.MinAttackEnergy.Value;
            float maxE = Mathf.Max(minE + 0.01f, ParryConfig.MaxAttackEnergy.Value);
            float normalized = Mathf.Clamp01((energy - minE) / (maxE - minE));
            float curve = Mathf.Sqrt(normalized);

            force = Mathf.Lerp(ParryConfig.MinExplosionForce.Value, ParryConfig.MaxExplosionForce.Value, curve);
            radius = Mathf.Lerp(ParryConfig.MinExplosionRadius.Value, ParryConfig.MaxExplosionRadius.Value, curve);
        }
    }
}
