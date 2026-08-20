using RepoValuableParry.Core;
using RepoValuableParry.Physics;
using UnityEngine;

namespace RepoValuableParry.Destruction
{
    internal static class ValuableSacrifice
    {
        public static SacrificedValuableData Capture(ValuableObject valuable, PhysGrabObject phys)
        {
            var go = valuable != null ? valuable.gameObject : phys != null ? phys.gameObject : null;
            var bounds = ParryCapacityCalculator.CombineColliderBounds(go);
            return new SacrificedValuableData
            {
                Name = go != null ? go.name.Replace("(Clone)", "").Trim() : "Valuable",
                DollarValue = GameAccess.GetDollarValue(valuable),
                Mass = ParryCapacityCalculator.GetMass(phys, valuable),
                Bounds = bounds,
                Position = go != null ? go.transform.position : Vector3.zero,
                Rotation = go != null ? go.transform.rotation : Quaternion.identity,
                Scale = go != null ? go.transform.lossyScale : Vector3.one
            };
        }

        public static void DestroyVanilla(ValuableObject valuable, PhysGrabObject phys)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer())
                return;

            var detector = phys != null
                ? phys.GetComponent<PhysGrabObjectImpactDetector>()
                : valuable != null ? valuable.GetComponent<PhysGrabObjectImpactDetector>() : null;

            if (detector == null)
            {
                if (phys != null)
                    Object.Destroy(phys.gameObject);
                else if (valuable != null)
                    Object.Destroy(valuable.gameObject);
                Plugin.LogWarning("Valuable had no PhysGrabObjectImpactDetector; used fallback destroy.");
                return;
            }

            detector.destroyDisable = false;
            detector.DestroyObject(true);
        }
    }
}
