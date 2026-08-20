using System.Collections.Generic;
using UnityEngine;

namespace RepoValuableParry.Effects
{
    internal static class ParryAudio
    {
        static AudioClip _thump;
        static AudioClip _hum;
        static AudioClip _overload;
        static AudioClip _boom;
        static bool _built;

        public static void PlayThump(Vector3 position, float intensity)
        {
            EnsureClips();
            Play(_thump, position, 0.85f * intensity, 0.7f);
        }

        public static void PlayHum(Vector3 position, float intensity)
        {
            EnsureClips();
            Play(_hum, position, 0.55f * intensity, 1f);
        }

        public static void PlayOverload(Vector3 position, float intensity)
        {
            EnsureClips();
            Play(_overload, position, 0.7f * intensity, 1.2f);
        }

        public static void PlayDetonation(Vector3 position, float intensity)
        {
            EnsureClips();
            Play(_boom, position, 1.1f * intensity, 0.55f + intensity * 0.15f);
        }

        static void Play(AudioClip clip, Vector3 position, float volume, float pitch)
        {
            if (clip == null)
                return;
            var go = new GameObject("ValuableParryAudio");
            go.transform.position = position;
            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1.5f;
            source.maxDistance = 22f;
            source.volume = Mathf.Clamp01(volume);
            source.pitch = Mathf.Clamp(pitch, 0.4f, 1.8f);
            source.Play();
            Object.Destroy(go, clip.length + 0.1f);
        }

        static void EnsureClips()
        {
            if (_built)
                return;
            _built = true;
            _thump = Build("parry-thump", 0.18f, (t, n) =>
            {
                float env = Mathf.Exp(-t * 18f);
                return (Mathf.Sin(t * 90f * Mathf.PI * 2f) * 0.7f + n * 0.3f) * env;
            });
            _hum = Build("parry-hum", 0.16f, (t, n) =>
            {
                float env = Mathf.Sin(Mathf.Clamp01(t / 0.16f) * Mathf.PI);
                float freq = Mathf.Lerp(180f, 420f, t / 0.16f);
                return (Mathf.Sin(t * freq * Mathf.PI * 2f) + n * 0.08f) * env * 0.5f;
            });
            _overload = Build("parry-overload", 0.08f, (t, n) =>
            {
                float env = t / 0.08f;
                float freq = Mathf.Lerp(400f, 1400f, env);
                return (Mathf.Sin(t * freq * Mathf.PI * 2f) + n * 0.15f) * env;
            });
            _boom = Build("parry-boom", 0.42f, (t, n) =>
            {
                float env = Mathf.Exp(-t * 7f);
                float low = Mathf.Sin(t * 55f * Mathf.PI * 2f);
                float mid = Mathf.Sin(t * 110f * Mathf.PI * 2f) * 0.4f;
                return (low + mid + n * 0.45f) * env;
            });
        }

        static AudioClip Build(string name, float duration, System.Func<float, float, float> sample)
        {
            const int hz = 22050;
            int count = Mathf.CeilToInt(duration * hz);
            var data = new float[count];
            var rng = new System.Random(name.GetHashCode());
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)hz;
                float n = (float)(rng.NextDouble() * 2.0 - 1.0);
                data[i] = Mathf.Clamp(sample(t, n), -1f, 1f);
            }
            var clip = AudioClip.Create(name, count, 1, hz, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
