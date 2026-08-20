using BepInEx.Configuration;

namespace RepoValuableParry
{
    internal static class ParryConfig
    {
        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<bool> UseVanillaInteract;
        public static ConfigEntry<UnityEngine.KeyCode> FallbackParryKey;
        public static ConfigEntry<float> ParryWindow;
        public static ConfigEntry<float> PlayerKnockbackMultiplier;
        public static ConfigEntry<float> EnemyKnockbackMultiplier;
        public static ConfigEntry<float> EffectsIntensity;
        public static ConfigEntry<float> CameraShake;
        public static ConfigEntry<bool> DebugOverlay;
        public static ConfigEntry<bool> DebugGizmos;
        public static ConfigEntry<bool> VerboseLogs;

        public static ConfigEntry<float> DamageWeight;
        public static ConfigEntry<float> ForceWeight;
        public static ConfigEntry<float> TumbleWeight;
        public static ConfigEntry<float> SizeWeight;
        public static ConfigEntry<float> MassWeight;
        public static ConfigEntry<float> SizeMultiplier;
        public static ConfigEntry<float> MassMultiplier;
        public static ConfigEntry<float> MinimumCoverage;
        public static ConfigEntry<float> MinimumArea;
        public static ConfigEntry<float> CoverageForgiveness;
        public static ConfigEntry<float> FreezeDuration;
        public static ConfigEntry<float> AbsorptionDuration;
        public static ConfigEntry<float> OverloadDuration;
        public static ConfigEntry<float> MinExplosionForce;
        public static ConfigEntry<float> MaxExplosionForce;
        public static ConfigEntry<float> MinExplosionRadius;
        public static ConfigEntry<float> MaxExplosionRadius;
        public static ConfigEntry<float> MinAttackEnergy;
        public static ConfigEntry<float> MaxAttackEnergy;
        public static ConfigEntry<float> PlayerTumbleTime;
        public static ConfigEntry<int> EnemyDirectDamage;

        public static void Bind(ConfigFile config)
        {
            const string user = "User";
            const string balance = "Balance";
            const string debug = "Debug";

            Enabled = config.Bind(user, "Enable Mod", true, "Master switch for Valuable Parry.");
            UseVanillaInteract = config.Bind(user, "Use Vanilla Interact", true, "If true, parry uses the vanilla Interact action (E by default, respects rebinds/controllers). If false, uses Fallback Parry Key.");
            FallbackParryKey = config.Bind(user, "Fallback Parry Key", UnityEngine.KeyCode.E, "Used only when Use Vanilla Interact is false.");
            ParryWindow = config.Bind(user, "Parry Window", 0.35f, new ConfigDescription("How long ParryIntent stays armed after pressing E, in seconds.", new AcceptableValueRange<float>(0.05f, 0.8f)));
            PlayerKnockbackMultiplier = config.Bind(user, "Player Knockback Multiplier", 1f, new ConfigDescription("Scales knockback applied to the parrying player.", new AcceptableValueRange<float>(0f, 3f)));
            EnemyKnockbackMultiplier = config.Bind(user, "Enemy Knockback Multiplier", 1f, new ConfigDescription("Scales knockback applied to the attacking enemy.", new AcceptableValueRange<float>(0f, 3f)));
            EffectsIntensity = config.Bind(user, "Effects Intensity", 1f, new ConfigDescription("Scales VFX and audio intensity.", new AcceptableValueRange<float>(0f, 2f)));
            CameraShake = config.Bind(user, "Camera Shake", 1f, new ConfigDescription("Scales camera shake on detonation.", new AcceptableValueRange<float>(0f, 2f)));

            DebugOverlay = config.Bind(debug, "Debug Overlay", false, "Shows live parry numbers. Off in a normal playthrough.");
            DebugGizmos = config.Bind(debug, "Debug Gizmos", false, "Draws attack line, valuable bounds, contact point and knockback vectors.");
            VerboseLogs = config.Bind(debug, "Verbose Logs", true, "Logs every reject reason and successful capture.");

            DamageWeight = config.Bind(balance, "Damage Weight", 1f, "AttackEnergy contribution from HurtCollider.playerDamage.");
            ForceWeight = config.Bind(balance, "Force Weight", 3f, "AttackEnergy contribution from HurtCollider.physHitForce / playerHitForce.");
            TumbleWeight = config.Bind(balance, "Tumble Weight", 4f, "AttackEnergy contribution from HurtCollider.playerTumbleForce.");
            SizeWeight = config.Bind(balance, "Size Weight", 0.7f, "Share of ParryCapacity that comes from projected area.");
            MassWeight = config.Bind(balance, "Mass Weight", 0.3f, "Share of ParryCapacity that comes from mass.");
            SizeMultiplier = config.Bind(balance, "Size Multiplier", 40f, "Projected area is multiplied by this before weighting.");
            MassMultiplier = config.Bind(balance, "Mass Multiplier", 15f, "sqrt(mass) is multiplied by this before weighting.");
            MinimumCoverage = config.Bind(balance, "Minimum Coverage", 0.25f, new ConfigDescription("Minimum fraction of the player torso that the valuable must cover.", new AcceptableValueRange<float>(0.05f, 0.9f)));
            MinimumArea = config.Bind(balance, "Minimum Area", 0.06f, "Hard minimum projected area in square metres.");
            CoverageForgiveness = config.Bind(balance, "Coverage Forgiveness", 0.4f, "Bounds padding in metres. Extra slack so a held valuable counts as covering a laser/melee line.");
            FreezeDuration = config.Bind(balance, "Freeze Duration", 0.18f, new ConfigDescription("How long the captured attack stays frozen, in seconds.", new AcceptableValueRange<float>(0.08f, 0.4f)));
            AbsorptionDuration = config.Bind(balance, "Absorption Duration", 0.14f, "Energy-in visual window after freeze starts.");
            OverloadDuration = config.Bind(balance, "Overload Duration", 0.03f, "Short flash before detonation.");
            MinExplosionForce = config.Bind(balance, "Min Explosion Force", 8f, "Knockback force at the weakest parryable hit.");
            MaxExplosionForce = config.Bind(balance, "Max Explosion Force", 28f, "Knockback force at the strongest parryable hit.");
            MinExplosionRadius = config.Bind(balance, "Min Explosion Radius", 1.4f, "Shockwave radius at the weakest parryable hit.");
            MaxExplosionRadius = config.Bind(balance, "Max Explosion Radius", 4.5f, "Shockwave radius at the strongest parryable hit.");
            MinAttackEnergy = config.Bind(balance, "Min Attack Energy", 8f, "Maps to min explosion scale.");
            MaxAttackEnergy = config.Bind(balance, "Max Attack Energy", 80f, "Maps to max explosion scale.");
            PlayerTumbleTime = config.Bind(balance, "Player Tumble Time", 1.35f, "How long the player stays tumbling after detonation.");
            EnemyDirectDamage = config.Bind(balance, "Enemy Direct Damage", 0, "Direct HP damage to the attacker. Keep at 0 so parry is not an offensive weapon.");
        }
    }
}
