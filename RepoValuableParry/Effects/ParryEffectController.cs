using UnityEngine;

namespace RepoValuableParry.Effects
{
    internal static class ParryEffectController
    {
        public static void PlayCapture(Vector3 contact, float intensity)
        {
            ParryAudio.PlayThump(contact, intensity);
        }

        public static GameObject PlayAbsorption(Vector3 contact, Transform valuable, float intensity, float duration)
        {
            ParryAudio.PlayHum(contact, intensity);
            return AbsorptionEffect.Spawn(contact, valuable, intensity, duration);
        }

        public static void PlayOverload(Vector3 position, float intensity)
        {
            ParryAudio.PlayOverload(position, intensity);
        }

        public static void PlayDetonation(Vector3 position, float radius, float intensity)
        {
            ParryAudio.PlayDetonation(position, intensity);
            DetonationEffect.Spawn(position, radius, intensity);
            CameraEffects.ShakeDetonation(position, intensity);
        }
    }
}
