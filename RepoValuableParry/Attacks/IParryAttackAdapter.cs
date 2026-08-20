using UnityEngine;

namespace RepoValuableParry.Attacks
{
    public interface IParryAttackAdapter
    {
        bool CanHandle(HurtCollider collider);
        bool IsAttackActive(HurtCollider collider);
        Enemy GetEnemy(HurtCollider collider);
        Vector3 GetAttackOrigin(HurtCollider collider);
        Vector3 GetAttackDirection(HurtCollider collider, PlayerAvatar target);
        float GetAttackEnergy(HurtCollider collider);
        Core.ParryAttackType GetAttackType(HurtCollider collider);
        void FreezeAttack(HurtCollider collider, Enemy enemy, float duration);
        void ConsumeAttack(HurtCollider collider);
        void ApplyPostParryReaction(Enemy enemy);
    }
}
