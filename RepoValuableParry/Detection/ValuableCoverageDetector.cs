using UnityEngine;

namespace RepoValuableParry.Detection
{
    internal static class ValuableCoverageDetector
    {
        public static bool CoversPlayer(
            Bounds valuableBounds,
            Vector3 attackOrigin,
            Vector3 playerBodyPoint,
            out float coverage,
            out Vector3 contactPoint)
        {
            coverage = 0f;
            contactPoint = valuableBounds.ClosestPoint(playerBodyPoint);

            Vector3 toPlayer = playerBodyPoint - attackOrigin;
            float distance = toPlayer.magnitude;
            if (distance < 0.05f)
                return false;

            Vector3 dir = toPlayer / distance;
            Vector3 valuableCenter = valuableBounds.center;
            float t = Vector3.Dot(valuableCenter - attackOrigin, dir);

            // Must be in front of the attacker. Allow the held object to sit
            // right on the player (grab point is often near the camera).
            if (t < -0.25f)
                return false;

            Vector3 closestOnSegment = attackOrigin + dir * Mathf.Clamp(t, 0f, distance);
            Vector3 closestOnValuable = valuableBounds.ClosestPoint(closestOnSegment);
            float miss = Vector3.Distance(closestOnSegment, closestOnValuable);
            float radius = valuableBounds.extents.magnitude * 0.5f + ParryConfig.CoverageForgiveness.Value + 0.25f;

            bool onLine = miss <= radius;
            if (valuableBounds.IntersectRay(new Ray(attackOrigin, dir), out float enter) && enter <= distance + 0.5f)
            {
                onLine = true;
                contactPoint = attackOrigin + dir * enter;
            }
            else
            {
                contactPoint = closestOnValuable;
            }

            coverage = TorsoCoverage(valuableBounds, playerBodyPoint, dir);
            if (onLine && coverage < ParryConfig.MinimumCoverage.Value)
                coverage = ParryConfig.MinimumCoverage.Value;

            return onLine;
        }

        /// <summary>
        /// Laser/beam parry: the valuable must sit in the beam volume
        /// (in front of the player), not at the enemy muzzle.
        /// </summary>
        public static bool CoversBeam(
            HurtCollider beamCollider,
            Bounds valuableBounds,
            Vector3 beamOrigin,
            Vector3 playerBodyPoint,
            out float coverage,
            out Vector3 contactPoint)
        {
            coverage = 0f;
            contactPoint = valuableBounds.ClosestPoint(playerBodyPoint);

            var col = beamCollider != null ? beamCollider.GetComponent<Collider>() : null;
            Vector3 start = beamOrigin;
            Vector3 end = playerBodyPoint;
            Vector3 forward = (end - start);
            float length = forward.magnitude;
            if (length > 0.05f)
                forward /= length;

            if (col != null)
            {
                Vector3 axis = col.transform.forward;
                if (axis.sqrMagnitude > 0.001f)
                {
                    start = col.bounds.center - axis.normalized * (col.bounds.size.magnitude * 0.5f);
                    // Prefer the real beam box: ClosestPoint is the actual laser volume.
                    Vector3 onBeam = col.ClosestPoint(valuableBounds.center);
                    contactPoint = valuableBounds.ClosestPoint(onBeam);
                    float miss = Vector3.Distance(onBeam, contactPoint);
                    float slack = ParryConfig.CoverageForgiveness.Value + 0.35f;
                    if (miss <= slack)
                    {
                        coverage = 1f;
                        return true;
                    }
                    forward = axis.normalized;
                    length = Mathf.Max(length, col.transform.lossyScale.z);
                    start = col.transform.position;
                }
            }

            var ray = new Ray(start, forward);
            if (valuableBounds.IntersectRay(ray, out float enter) && enter >= -0.5f && enter <= length + 0.5f)
            {
                contactPoint = start + forward * Mathf.Max(0f, enter);
                coverage = 1f;
                return true;
            }

            Vector3 toValuable = valuableBounds.center - start;
            float t = Vector3.Dot(toValuable, forward);
            if (t < -0.25f || t > length + 0.75f)
                return CoversPlayer(valuableBounds, beamOrigin, playerBodyPoint, out coverage, out contactPoint);

            Vector3 onLine = start + forward * t;
            contactPoint = valuableBounds.ClosestPoint(onLine);
            float lineMiss = Vector3.Distance(onLine, contactPoint);
            float radius = valuableBounds.extents.magnitude * 0.35f + ParryConfig.CoverageForgiveness.Value + 0.35f;
            bool hit = lineMiss <= radius;
            if (hit)
                coverage = 1f;
            return hit;
        }

        /// <summary>
        /// Body-contact / crawler: attack comes from the enemy body at your
        /// feet, not a swing at chest height. Cover is horizontal: the
        /// valuable is between you and the mob on the floor plane, or close
        /// enough that you are shoving it into the mob.
        /// </summary>
        public static bool CoversContact(
            Bounds valuableBounds,
            Vector3 enemyPosition,
            Vector3 playerBodyPoint,
            out float coverage,
            out Vector3 contactPoint)
        {
            coverage = 0f;
            contactPoint = valuableBounds.ClosestPoint(playerBodyPoint);

            Vector3 valuable = valuableBounds.center;
            Vector2 enemy2 = new Vector2(enemyPosition.x, enemyPosition.z);
            Vector2 player2 = new Vector2(playerBodyPoint.x, playerBodyPoint.z);
            Vector2 valuable2 = new Vector2(valuable.x, valuable.z);
            Vector2 toPlayer = player2 - enemy2;
            float dist = toPlayer.magnitude;
            float slack = ParryConfig.CoverageForgiveness.Value + 0.7f;
            float reach = valuableBounds.extents.magnitude + slack;

            if (dist < 1.6f)
            {
                float toEnemy = Vector2.Distance(valuable2, enemy2);
                float toPlayerD = Vector2.Distance(valuable2, player2);
                bool closeEnough = toEnemy <= 2.4f || toPlayerD <= 2.2f;
                if (closeEnough)
                {
                    coverage = 1f;
                    contactPoint = valuableBounds.ClosestPoint(enemyPosition);
                    return true;
                }
            }

            if (dist < 0.05f)
                return false;

            Vector2 dir = toPlayer / dist;
            float t = Vector2.Dot(valuable2 - enemy2, dir);
            if (t < -0.4f || t > dist + 0.6f)
                return false;

            Vector2 onLine = enemy2 + dir * t;
            float miss = Vector2.Distance(onLine, valuable2);
            if (miss <= reach)
            {
                coverage = 1f;
                contactPoint = valuable;
                return true;
            }
            return false;
        }

        static float TorsoCoverage(Bounds valuableBounds, Vector3 playerBodyPoint, Vector3 attackDir)
        {
            var torso = new Bounds(playerBodyPoint, new Vector3(0.7f, 0.9f, 0.6f));
            Vector3 axisA = Vector3.Cross(attackDir, Vector3.up);
            if (axisA.sqrMagnitude < 0.01f)
                axisA = Vector3.Cross(attackDir, Vector3.right);
            axisA.Normalize();
            Vector3 axisB = Vector3.Cross(attackDir, axisA).normalized;

            ProjectAabb(torso, axisA, axisB, out Vector2 tMin, out Vector2 tMax);
            ProjectAabb(valuableBounds, axisA, axisB, out Vector2 vMin, out Vector2 vMax);

            Vector2 overlapMin = Vector2.Max(tMin, vMin);
            Vector2 overlapMax = Vector2.Min(tMax, vMax);
            Vector2 overlap = overlapMax - overlapMin;
            if (overlap.x <= 0f || overlap.y <= 0f)
                return 0f;

            float torsoArea = Mathf.Max(0.0001f, (tMax.x - tMin.x) * (tMax.y - tMin.y));
            return Mathf.Clamp01((overlap.x * overlap.y) / torsoArea);
        }

        static void ProjectAabb(Bounds bounds, Vector3 axisA, Vector3 axisB, out Vector2 min, out Vector2 max)
        {
            Vector3 c = bounds.center;
            Vector3 e = bounds.extents;
            Vector3[] corners =
            {
                c + new Vector3( e.x,  e.y,  e.z),
                c + new Vector3( e.x,  e.y, -e.z),
                c + new Vector3( e.x, -e.y,  e.z),
                c + new Vector3( e.x, -e.y, -e.z),
                c + new Vector3(-e.x,  e.y,  e.z),
                c + new Vector3(-e.x,  e.y, -e.z),
                c + new Vector3(-e.x, -e.y,  e.z),
                c + new Vector3(-e.x, -e.y, -e.z)
            };

            min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (var corner in corners)
            {
                var p = new Vector2(Vector3.Dot(corner, axisA), Vector3.Dot(corner, axisB));
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }
        }
    }
}
