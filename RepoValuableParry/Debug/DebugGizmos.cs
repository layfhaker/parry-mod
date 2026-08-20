using RepoValuableParry.Core;
using UnityEngine;

namespace RepoValuableParry.DebugTools
{
    internal sealed class DebugGizmos : MonoBehaviour
    {
        LineRenderer _attack;
        LineRenderer _knockEnemy;
        LineRenderer _knockPlayer;
        GameObject _boundsBox;
        GameObject _contact;

        void Awake()
        {
            _attack = MakeLine("AttackLine", new Color(1f, 0.15f, 0.15f, 0.95f), 0.02f);
            _knockEnemy = MakeLine("EnemyKnock", new Color(0.25f, 0.45f, 1f, 0.95f), 0.025f);
            _knockPlayer = MakeLine("PlayerKnock", new Color(0.25f, 0.45f, 1f, 0.95f), 0.025f);
            _boundsBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _boundsBox.name = "ValuableBounds";
            _boundsBox.transform.SetParent(transform, false);
            Object.Destroy(_boundsBox.GetComponent<Collider>());
            var r = _boundsBox.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Hidden/InternalErrorShader"));
            mat.color = new Color(0.15f, 1f, 0.25f, 0.18f);
            r.material = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _contact = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _contact.name = "Contact";
            _contact.transform.SetParent(transform, false);
            _contact.transform.localScale = Vector3.one * 0.12f;
            Object.Destroy(_contact.GetComponent<Collider>());
            var cr = _contact.GetComponent<MeshRenderer>();
            var cmat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Hidden/InternalErrorShader"));
            cmat.color = new Color(1f, 0.92f, 0.15f, 0.9f);
            cr.material = cmat;
            SetVisible(false);
        }

        void LateUpdate()
        {
            bool on = ParryConfig.DebugGizmos.Value || ParryConfig.DebugOverlay.Value;
            if (!on || ParryManager.Instance == null)
            {
                SetVisible(false);
                return;
            }

            var ctx = ParryManager.Instance.LastContext;
            var attack = ParryManager.Instance.LastAttack;
            var stats = ParryManager.Instance.LastStats;
            bool hasAttack = attack.Collider != null || attack.Origin != Vector3.zero;
            if (!hasAttack && ctx == null)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            Vector3 origin = ctx != null ? ctx.AttackOrigin : attack.Origin;
            Vector3 contact = ctx != null ? ctx.ContactPoint : attack.ContactPoint;
            Vector3 playerPos = ctx != null && ctx.Player != null
                ? ctx.Player.transform.position
                : SemiFunc.PlayerGetLocal() != null ? SemiFunc.PlayerGetLocal().transform.position : origin;
            Vector3 enemyPos = ctx != null && ctx.Enemy != null ? ctx.Enemy.transform.position : origin - Vector3.forward;

            SetLine(_attack, origin, playerPos);
            if (stats.Bounds.size.sqrMagnitude > 0.0001f)
            {
                _boundsBox.transform.position = stats.Bounds.center;
                _boundsBox.transform.rotation = Quaternion.identity;
                _boundsBox.transform.localScale = stats.Bounds.size;
            }
            _contact.transform.position = contact == Vector3.zero ? stats.Bounds.center : contact;

            if (ctx != null)
            {
                SetLine(_knockEnemy, ctx.ContactPoint, enemyPos);
                SetLine(_knockPlayer, ctx.ContactPoint, playerPos);
            }
        }

        void SetVisible(bool visible)
        {
            if (_attack != null) _attack.enabled = visible;
            if (_knockEnemy != null) _knockEnemy.enabled = visible;
            if (_knockPlayer != null) _knockPlayer.enabled = visible;
            if (_boundsBox != null) _boundsBox.SetActive(visible);
            if (_contact != null) _contact.SetActive(visible);
        }

        static void SetLine(LineRenderer lr, Vector3 a, Vector3 b)
        {
            if (lr == null)
                return;
            lr.positionCount = 2;
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
        }

        LineRenderer MakeLine(string name, Color color, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.widthMultiplier = width;
            var mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Hidden/InternalErrorShader"));
            mat.color = color;
            lr.material = mat;
            lr.startColor = color;
            lr.endColor = color;
            return lr;
        }
    }
}
