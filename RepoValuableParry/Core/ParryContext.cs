using UnityEngine;

namespace RepoValuableParry.Core
{
    public sealed class ParryContext
    {
        public PlayerAvatar Player;
        public PhysGrabObject PhysObject;
        public ValuableObject Valuable;
        public HurtCollider AttackCollider;
        public Enemy Enemy;
        public Vector3 AttackOrigin;
        public Vector3 AttackDirection;
        public Vector3 ContactPoint;
        public float AttackEnergy;
        public float ValuableCapacity;
        public float ExplosionForce;
        public float ExplosionRadius;
        public int SequenceId;
        public int EffectSeed;
        public ValuableStats Stats;
        public AttackData Attack;
        public SacrificedValuableData Sacrificed;
        public bool Captured;
        public bool ValuableLocked;
        public Attacks.IParryAttackAdapter Adapter;
    }
}
