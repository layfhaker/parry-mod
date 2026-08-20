namespace RepoValuableParry.Core
{
    public enum ParryRejectReason
    {
        None,
        NoIntent,
        IntentExpired,
        NoHeldObject,
        HeldObjectChanged,
        NotValuable,
        UnsupportedAttack,
        NoEnemySource,
        NotCoveringPlayer,
        TooSmall,
        InsufficientCapacity,
        AttackAlreadyConsumed,
        NetworkAuthority,
        ModDisabled,
        PlayerDead,
        WrongPlayer
    }

    public enum ParryAttackType
    {
        Melee,
        Charge,
        Bite,
        BodySlam,
        Grab,
        Projectile,
        Explosion,
        Environmental,
        Special
    }
}
