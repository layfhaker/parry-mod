using UnityEngine;

namespace RepoValuableParry.Effects
{
    internal static class AbsorptionEffect
    {
        public static GameObject Spawn(Vector3 contact, Transform valuable, float intensity, float duration)
        {
            var root = new GameObject("ValuableParryAbsorption");
            root.transform.position = contact;

            var lightGo = new GameObject("Glow");
            lightGo.transform.SetParent(root.transform, false);
            if (valuable != null)
                lightGo.transform.position = valuable.position;
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.45f, 0.9f, 1f);
            light.range = 8f + intensity * 4f;
            light.intensity = 8f * intensity;

            var psGo = new GameObject("PullParticles");
            psGo.transform.SetParent(root.transform, false);
            psGo.transform.position = contact;
            var ps = psGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = Mathf.Max(0.08f, duration);
            main.startSpeed = 0.2f;
            main.startSize = 0.05f * intensity;
            main.startColor = new Color(0.7f, 0.95f, 1f, 0.85f);
            main.maxParticles = 64;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 80f * intensity;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;
            var trails = ps.trails;
            trails.enabled = true;
            trails.lifetime = 0.15f;
            var renderer = psGo.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            var particleMat = ParryShaders.Make(new Color(0.7f, 0.95f, 1f, 0.9f));
            if (particleMat != null)
                renderer.material = particleMat;

            var driver = root.AddComponent<AbsorptionDriver>();
            driver.Init(contact, valuable, light, duration, intensity);
            Object.Destroy(root, duration + 0.4f);
            return root;
        }

        class AbsorptionDriver : MonoBehaviour
        {
            Vector3 _contact;
            Transform _valuable;
            Light _light;
            float _duration;
            float _intensity;
            float _age;
            LineRenderer[] _arcs;

            public void Init(Vector3 contact, Transform valuable, Light light, float duration, float intensity)
            {
                _contact = contact;
                _valuable = valuable;
                _light = light;
                _duration = duration;
                _intensity = intensity;
                _arcs = new LineRenderer[3];
                for (int i = 0; i < _arcs.Length; i++)
                {
                    var go = new GameObject("Arc" + i);
                    go.transform.SetParent(transform, false);
                    var lr = go.AddComponent<LineRenderer>();
                    lr.positionCount = 6;
                    lr.widthMultiplier = 0.08f * intensity;
                    lr.useWorldSpace = true;
                    var mat = ParryShaders.Make(new Color(0.55f, 0.95f, 1f, 1f));
                    if (mat != null)
                        lr.material = mat;
                    lr.startColor = new Color(1f, 1f, 1f, 1f);
                    lr.endColor = new Color(0.3f, 0.8f, 1f, 0.4f);
                    _arcs[i] = lr;
                }
            }

            void Update()
            {
                _age += Time.deltaTime;
                Vector3 target = _valuable != null ? _valuable.position : _contact;
                float t = Mathf.Clamp01(_age / Mathf.Max(0.01f, _duration));
                if (_light != null)
                {
                    _light.transform.position = target;
                    _light.intensity = Mathf.Lerp(6f, 18f * _intensity, t);
                    _light.color = Color.Lerp(new Color(0.55f, 0.85f, 1f), new Color(1f, 0.95f, 0.7f), t);
                }

                for (int i = 0; i < _arcs.Length; i++)
                {
                    if (_arcs[i] == null)
                        continue;
                    for (int p = 0; p < 6; p++)
                    {
                        float u = p / 5f;
                        Vector3 point = Vector3.Lerp(_contact, target, u);
                        float wobble = Mathf.Sin((Time.time * (8f + i * 3f)) + p) * 0.08f * (1f - Mathf.Abs(u * 2f - 1f));
                        Vector3 side = Vector3.Cross((target - _contact).normalized, Vector3.up);
                        if (side.sqrMagnitude < 0.01f)
                            side = Vector3.right;
                        point += side.normalized * wobble + Vector3.up * wobble * 0.4f;
                        _arcs[i].SetPosition(p, point);
                    }
                    _arcs[i].widthMultiplier = Mathf.Lerp(0.05f, 0.14f, t) * _intensity;
                }
            }
        }
    }
}
