using UnityEngine;

namespace RepoValuableParry.Core
{
    public struct ValuableStats
    {
        public string Name;
        public float Mass;
        public float DollarValue;
        public Bounds Bounds;
        public float ProjectedArea;
        public float Coverage;
        public float Capacity;
        public bool MeetsMinimumSize;
    }

    public struct SacrificedValuableData
    {
        public string Name;
        public float DollarValue;
        public float Mass;
        public Bounds Bounds;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
    }

    public struct AttackData
    {
        public int ColliderId;
        public Enemy Enemy;
        public HurtCollider Collider;
        public Vector3 Origin;
        public Vector3 Direction;
        public Vector3 ContactPoint;
        public float Energy;
        public int PlayerDamage;
        public float PhysHitForce;
        public float PlayerTumbleForce;
        public ParryAttackType AttackType;
    }
}
