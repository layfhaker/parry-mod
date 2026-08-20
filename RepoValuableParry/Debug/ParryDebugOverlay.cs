using RepoValuableParry.Core;
using UnityEngine;

namespace RepoValuableParry.DebugTools
{
    internal sealed class ParryDebugOverlay : MonoBehaviour
    {
        GUIStyle _box;
        GUIStyle _label;
        GUIStyle _title;

        void OnGUI()
        {
            if (ParryConfig.DebugOverlay == null || !ParryConfig.DebugOverlay.Value || ParryManager.Instance == null)
                return;

            EnsureStyles();
            var mgr = ParryManager.Instance;
            var stats = mgr.LastStats;
            var attack = mgr.LastAttack;

            const float w = 340f;
            const float h = 280f;
            GUI.Box(new Rect(16f, 16f, w, h), GUIContent.none, _box);

            float y = 24f;
            GUI.Label(new Rect(28f, y, w - 40f, 22f), "VALUABLE PARRY", _title);
            y += 26f;
            GUI.Label(new Rect(28f, y, w - 40f, 20f), $"Valuable: {(string.IsNullOrEmpty(stats.Name) ? "-" : stats.Name)}", _label);
            y += 18f;
            GUI.Label(new Rect(28f, y, w - 40f, 20f), $"Mass: {stats.Mass:0.00}    Area: {stats.ProjectedArea:0.00}", _label);
            y += 18f;
            GUI.Label(new Rect(28f, y, w - 40f, 20f), $"Coverage: {stats.Coverage:P0}    Capacity: {stats.Capacity:0.0}", _label);
            y += 18f;
            GUI.Label(new Rect(28f, y, w - 40f, 20f), $"Dollar value (not used): {stats.DollarValue:0}", _label);
            y += 26f;
            GUI.Label(new Rect(28f, y, w - 40f, 20f), $"Enemy: {mgr.LastEnemyName}", _label);
            y += 18f;
            GUI.Label(new Rect(28f, y, w - 40f, 20f), $"Damage: {attack.PlayerDamage}    Hit Force: {attack.PhysHitForce:0.00}", _label);
            y += 18f;
            GUI.Label(new Rect(28f, y, w - 40f, 20f), $"Tumble Force: {attack.PlayerTumbleForce:0.00}", _label);
            y += 18f;
            GUI.Label(new Rect(28f, y, w - 40f, 20f), $"Attack Energy: {attack.Energy:0.0}", _label);
            y += 26f;

            string result = mgr.LastWouldParry ? "PARRY POSSIBLE / CAPTURED" : "NO PARRY  (" + mgr.LastReject + ")";
            GUI.Label(new Rect(28f, y, w - 40f, 22f), result, _title);
        }

        void EnsureStyles()
        {
            if (_box != null)
                return;
            _box = new GUIStyle(GUI.skin.box);
            _label = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            _title = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
        }
    }
}
