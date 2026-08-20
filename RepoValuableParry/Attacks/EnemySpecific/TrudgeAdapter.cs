using RepoValuableParry.Core;

namespace RepoValuableParry.Attacks.EnemySpecific
{
    internal sealed class TrudgeAdapter : GenericMeleeAdapter
    {
        public override bool CanHandle(HurtCollider collider)
        {
            if (!base.CanHandle(collider))
                return false;
            return collider.GetComponentInParent<EnemySlowWalker>() != null;
        }

        public override ParryAttackType GetAttackType(HurtCollider collider)
        {
            return collider.playerTumbleForce > 8f || collider.playerDamage >= 30
                ? ParryAttackType.Charge
                : ParryAttackType.BodySlam;
        }
    }
}
