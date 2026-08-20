using UnityEngine;

namespace RepoValuableParry.Networking
{
    public struct ParryRequest
    {
        public int PlayerViewId;
        public int ValuableViewId;
        public int InputSequence;
        public float ClientTime;
    }

    public struct ParryEvent
    {
        public int SequenceId;
        public int PlayerViewId;
        public int EnemyViewId;
        public int ValuableViewId;
        public float AttackEnergy;
        public Vector3 ContactPoint;
        public int EffectSeed;
    }
}
