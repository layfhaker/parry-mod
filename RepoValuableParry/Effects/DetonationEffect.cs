using UnityEngine;

namespace RepoValuableParry.Effects
{
    internal static class DetonationEffect
    {
        public static void Spawn(Vector3 position, float radius, float intensity)
        {
            var root = new GameObject("ValuableParryDetonation");
            root.transform.position = position;

            var lightGo = new GameObject("Flash");
            lightGo.transform.SetParent(root.transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.95f, 0.75f);
            light.range = Mathf.Max(10f, radius * 3f);
            light.intensity = 22f * intensity;

            var psGo = new GameObject("Burst");
            psGo.transform.SetParent(root.transform, false);
            var ps = psGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.4f;
            main.loop = false;
            main.startLifetime = 0.35f;
            main.startSpeed = 4f * intensity;
            main.startSize = 0.08f * intensity;
            main.startColor = new Color(0.85f, 0.95f, 1f, 1f);
            main.maxParticles = 80;
            main.gravityModifier = 0.2f;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.RoundToInt(40 * intensity)) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;
            var renderer = psGo.GetComponent<ParticleSystemRenderer>();
            var mat = ParryShaders.Make(Color.white);
            if (mat != null)
                renderer.material = mat;

            var ring = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ring.name = "Shockwave";
            ring.transform.SetParent(root.transform, false);
            ring.transform.localScale = Vector3.one * 0.2f;
            Object.Destroy(ring.GetComponent<Collider>());
            var ringRenderer = ring.GetComponent<MeshRenderer>();
            var ringMat = ParryShaders.Make(new Color(0.75f, 0.95f, 1f, 0.55f));
            if (ringMat == null)
                ringMat = new Material(Shader.Find("Sprites/Default"));
            ringMat.color = new Color(0.75f, 0.95f, 1f, 0.55f);
            ringRenderer.material = ringMat;
            ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            root.AddComponent<DetonationDriver>().Init(light, ring.transform, radius, intensity);
            Object.Destroy(root, 0.7f);
        }

        class DetonationDriver : MonoBehaviour
        {
            Light _light;
            Transform _ring;
            float _radius;
            float _intensity;
            float _age;

            public void Init(Light light, Transform ring, float radius, float intensity)
            {
                _light = light;
                _ring = ring;
                _radius = radius;
                _intensity = intensity;
            }

            void Update()
            {
                _age += Time.deltaTime;
                float t = Mathf.Clamp01(_age / 0.35f);
                if (_light != null)
                    _light.intensity = Mathf.Lerp(22f * _intensity, 0f, t);
                if (_ring != null)
                {
                    float size = Mathf.Lerp(0.35f, Mathf.Max(3f, _radius * 3.2f), Mathf.Sqrt(t));
                    _ring.localScale = Vector3.one * size;
                    var r = _ring.GetComponent<MeshRenderer>();
                    if (r != null && r.material != null)
                    {
                        var c = r.material.color;
                        c.a = Mathf.Lerp(0.35f, 0f, t);
                        r.material.color = c;
                    }
                }
            }
        }
    }
}
