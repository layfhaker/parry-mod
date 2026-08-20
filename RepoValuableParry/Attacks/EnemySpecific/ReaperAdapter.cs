using RepoValuableParry.Core;
using UnityEngine;

namespace RepoValuableParry.Attacks.EnemySpecific
{
    internal sealed class ReaperAdapter : GenericMeleeAdapter
    {
        public override bool CanHandle(HurtCollider collider)
        {
            if (!base.CanHandle(collider))
                return false;
            return collider.GetComponentInParent<EnemyRunner>() != null;
        }

        public override ParryAttackType GetAttackType(HurtCollider collider)
        {
            return ParryAttackType.Melee;
        }

        public override Vector3 GetAttackOrigin(HurtCollider collider)
        {
            var runner = collider.GetComponentInParent<EnemyRunner>();
            if (runner != null && runner.hurtCollider != null)
            {
                var col = runner.hurtCollider.GetComponent<Collider>();
                if (col != null)
                    return col.bounds.center;
            }
            return base.GetAttackOrigin(collider);
        }
    }
}
