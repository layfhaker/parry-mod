using RepoValuableParry.Core;
using RepoValuableParry.Detection;
using RepoValuableParry.Physics;
using UnityEngine;

namespace RepoValuableParry.Attacks
{
    internal class GenericMeleeAdapter : IParryAttackAdapter
    {
        public virtual bool CanHandle(HurtCollider collider)
        {
            if (collider == null)
                return false;
            if (!collider.playerLogic)
                return false;
            if (collider.deathPit)
                return false;
            if (collider.playerKill)
                return false;
            if (collider.enemyHost == null)
                return false;
            if (collider.playerDamage <= 0 && collider.playerHitForce <= 0f && collider.playerTumbleForce <= 0f)
                return false;
            if (LooksLikeProjectile(collider))
                return false;
            return true;
        }

        public virtual bool IsAttackActive(HurtCollider collider)
        {
            return collider != null && collider.isActiveAndEnabled && collider.gameObject.activeInHierarchy;
        }

        public virtual Enemy GetEnemy(HurtCollider collider)
        {
            if (collider == null)
                return null;
            if (collider.enemyHost != null)
                return collider.enemyHost;
            return collider.GetComponentInParent<Enemy>();
        }

        public virtual Vector3 GetAttackOrigin(HurtCollider collider)
        {
            var enemy = GetEnemy(collider);
            if (enemy != null)
            {
                if (enemy.CenterTransform != null)
                    return enemy.CenterTransform.position;
                return enemy.transform.position;
            }

            var col = collider.GetComponent<Collider>();
            if (col != null)
                return col.bounds.center;
            return collider.transform.position;
        }

        public virtual Vector3 GetAttackDirection(HurtCollider collider, PlayerAvatar target)
        {
            Vector3 origin = GetAttackOrigin(collider);
            Vector3 body = GetPlayerBodyPoint(target);
            Vector3 dir = body - origin;
            if (dir.sqrMagnitude < 0.0001f)
                dir = collider.transform.forward;
            return dir.normalized;
        }

        public virtual bool IsValuableBlocking(
            HurtCollider collider,
            Bounds valuableBounds,
            PlayerAvatar player,
            out float coverage,
            out Vector3 contactPoint)
        {
            Vector3 origin = GetAttackOrigin(collider);
            Vector3 body = GetPlayerBodyPoint(player);
            if (ValuableCoverageDetector.CoversPlayer(valuableBounds, origin, body, out coverage, out contactPoint))
                return true;

            // Crawlers / body-slams hit from the floor or the whole body.
            // A 3D chest-ray misses a valuable held in front of the camera.
            float range = Vector3.Distance(origin, player != null ? player.transform.position : body);
            if (range <= 3.8f)
                return ValuableCoverageDetector.CoversContact(valuableBounds, origin, body, out coverage, out contactPoint);

            return false;
        }

        public virtual float GetAttackEnergy(HurtCollider collider)
        {
            return AttackEnergyCalculator.Compute(collider);
        }

        public virtual ParryAttackType GetAttackType(HurtCollider collider)
        {
            return ParryAttackType.Melee;
        }

        public virtual void FreezeAttack(HurtCollider collider, Enemy enemy, float duration)
        {
            if (enemy != null)
                enemy.Freeze(duration);
        }

        public virtual void ConsumeAttack(HurtCollider collider)
        {
            if (collider == null)
                return;
            collider.enabled = false;
        }

        public virtual void ApplyPostParryReaction(Enemy enemy)
        {
            GameAccess.TryStun(enemy, 0.45f);
        }

        public static Vector3 GetPlayerBodyPoint(PlayerAvatar player)
        {
            if (player == null)
                return Vector3.zero;
            if (player.PlayerVisionTarget != null && player.PlayerVisionTarget.VisionTransform != null)
                return player.PlayerVisionTarget.VisionTransform.position;
            return player.transform.position + Vector3.up * 1.1f;
        }

        static bool LooksLikeProjectile(HurtCollider collider)
        {
            if (collider.hasTimer && collider.destroyOnTimerEnd)
                return true;
            if (collider.GetComponentInParent<ItemGunBullet>() != null)
                return true;
            if (collider.GetComponentInParent<ItemGrenade>() != null)
                return true;
            return false;
        }
    }
}
