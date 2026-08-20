using RepoValuableParry.Core;
using UnityEngine;

namespace RepoValuableParry.Physics
{
    internal static class ParryCapacityCalculator
    {
        public static ValuableStats Evaluate(ValuableObject valuable, PhysGrabObject phys, Vector3 attackDirection)
        {
            var stats = new ValuableStats
            {
                Name = valuable != null ? StripClone(valuable.gameObject.name) : "none",
                DollarValue = GameAccess.GetDollarValue(valuable),
                Mass = GetMass(phys, valuable)
            };

            stats.Bounds = CombineColliderBounds(phys != null ? phys.gameObject : valuable != null ? valuable.gameObject : null);
            Vector3 dir = attackDirection.sqrMagnitude > 0.0001f ? attackDirection.normalized : Vector3.forward;
            stats.ProjectedArea = ProjectedArea(stats.Bounds.size, dir);
            stats.MeetsMinimumSize = stats.ProjectedArea >= ParryConfig.MinimumArea.Value;

            float sizeScore = stats.ProjectedArea * ParryConfig.SizeMultiplier.Value;
            float massScore = Mathf.Sqrt(Mathf.Max(stats.Mass, 0f)) * ParryConfig.MassMultiplier.Value;
            stats.Capacity =
                sizeScore * ParryConfig.SizeWeight.Value +
                massScore * ParryConfig.MassWeight.Value;

            return stats;
        }

        public static float GetMass(PhysGrabObject phys, ValuableObject valuable)
        {
            if (phys != null && phys.massOriginal > 0f)
                return phys.massOriginal;
            if (phys != null && phys.rb != null)
                return phys.rb.mass;
            if (valuable != null && valuable.physAttributePreset != null)
                return valuable.physAttributePreset.mass;
            return 1f;
        }

        public static float ProjectedArea(Vector3 size, Vector3 dir)
        {
            dir = new Vector3(Mathf.Abs(dir.x), Mathf.Abs(dir.y), Mathf.Abs(dir.z));
            float axy = size.x * size.y;
            float axz = size.x * size.z;
            float ayz = size.y * size.z;
            return axy * dir.z + axz * dir.y + ayz * dir.x;
        }

        public static Bounds CombineColliderBounds(GameObject go)
        {
            if (go == null)
                return new Bounds(Vector3.zero, Vector3.one * 0.1f);

            var colliders = go.GetComponentsInChildren<Collider>();
            bool started = false;
            Bounds bounds = default;
            foreach (var col in colliders)
            {
                if (col == null || !col.enabled || col.isTrigger)
                    continue;
                if (!started)
                {
                    bounds = col.bounds;
                    started = true;
                }
                else
                {
                    bounds.Encapsulate(col.bounds);
                }
            }

            if (!started)
                bounds = new Bounds(go.transform.position, go.transform.lossyScale);

            float pad = ParryConfig.CoverageForgiveness.Value;
            bounds.Expand(pad * 2f);
            return bounds;
        }

        static string StripClone(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Valuable";
            return name.Replace("(Clone)", "").Trim();
        }
    }
}
