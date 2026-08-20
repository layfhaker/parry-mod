using UnityEngine;

namespace RepoValuableParry.Effects
{
    internal static class CameraEffects
    {
        public static void ShakeDetonation(Vector3 position, float intensity)
        {
            float scale = Mathf.Max(0f, intensity) * ParryConfig.CameraShake.Value;
            if (scale <= 0.01f)
                return;
            SemiFunc.CameraShakeImpactDistance(position, 5.5f * scale, 0.4f, 0.5f, 16f);
            SemiFunc.CameraShakeDistance(position, 3.2f * scale, 0.5f, 1f, 18f);
        }
    }
}
