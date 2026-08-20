using HarmonyLib;
using RepoValuableParry.Core;
using RepoValuableParry.Detection;
using UnityEngine;

namespace RepoValuableParry.Attacks.EnemySpecific
{
    internal sealed class BeamerAdapter : GenericMeleeAdapter
    {
        public override bool CanHandle(HurtCollider collider)
        {
            if (collider == null || !collider.playerLogic || collider.deathPit || collider.playerKill)
                return false;
            return FindBeamer(collider) != null;
        }

        public override ParryAttackType GetAttackType(HurtCollider collider)
        {
            return ParryAttackType.Projectile;
        }

        public override Vector3 GetAttackOrigin(HurtCollider collider)
        {
            var beamer = FindBeamer(collider);
            if (beamer != null && beamer.laserStartTransform != null)
                return beamer.laserStartTransform.position;
            return base.GetAttackOrigin(collider);
        }

        public override bool IsValuableBlocking(
            HurtCollider collider,
            Bounds valuableBounds,
            PlayerAvatar player,
            out float coverage,
            out Vector3 contactPoint)
        {
            return ValuableCoverageDetector.CoversBeam(
                collider,
                valuableBounds,
                GetAttackOrigin(collider),
                GetPlayerBodyPoint(player),
                out coverage,
                out contactPoint);
        }

        public override void ConsumeAttack(HurtCollider collider)
        {
            var beamer = FindBeamer(collider);
            if (beamer != null)
            {
                var update = AccessTools.Method(typeof(EnemyBeamer), "UpdateState", new[] { typeof(EnemyBeamer.State) });
                update?.Invoke(beamer, new object[] { EnemyBeamer.State.Stun });

                var hurts = beamer.GetComponentsInChildren<HurtCollider>(true);
                foreach (var hurt in hurts)
                {
                    if (hurt == null)
                        continue;
                    hurt.enabled = false;
                    hurt.gameObject.SetActive(false);
                }
            }

            if (collider != null)
            {
                collider.enabled = false;
                collider.gameObject.SetActive(false);
            }
        }

        public override void FreezeAttack(HurtCollider collider, Enemy enemy, float duration)
        {
            base.FreezeAttack(collider, enemy, duration);
            ConsumeAttack(collider);
        }

        public static EnemyBeamer FindBeamer(HurtCollider collider)
        {
            if (collider == null)
                return null;
            var beamer = collider.GetComponentInParent<EnemyBeamer>();
            if (beamer != null)
                return beamer;
            if (collider.enemyHost != null)
                return collider.enemyHost.GetComponent<EnemyBeamer>()
                    ?? collider.enemyHost.GetComponentInParent<EnemyBeamer>()
                    ?? collider.enemyHost.GetComponentInChildren<EnemyBeamer>();
            return null;
        }
    }
}
