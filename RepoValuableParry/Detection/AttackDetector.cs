using System.Collections.Generic;
using RepoValuableParry.Attacks;
using RepoValuableParry.Attacks.EnemySpecific;
using UnityEngine;

namespace RepoValuableParry.Detection
{
    internal static class AttackDetector
    {
        static readonly List<IParryAttackAdapter> Adapters = new List<IParryAttackAdapter>
        {
            new BeamerAdapter(),
            new ReaperAdapter(),
            new TrudgeAdapter(),
            new GenericMeleeAdapter()
        };

        public static IParryAttackAdapter FindAdapter(HurtCollider collider)
        {
            foreach (var adapter in Adapters)
            {
                if (adapter.CanHandle(collider))
                    return adapter;
            }
            return null;
        }
    }
}
