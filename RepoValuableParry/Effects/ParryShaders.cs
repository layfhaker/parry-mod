using UnityEngine;

namespace RepoValuableParry.Effects
{
    internal static class ParryShaders
    {
        static Shader _cached;
        static bool _searched;

        public static Shader Unlit
        {
            get
            {
                if (_searched)
                    return _cached;
                _searched = true;
                string[] names =
                {
                    "Universal Render Pipeline/Unlit",
                    "Universal Render Pipeline/Particles/Unlit",
                    "Sprites/Default",
                    "UI/Default",
                    "Unlit/Color",
                    "Unlit/Transparent",
                    "Particles/Standard Unlit",
                    "Hidden/InternalErrorShader"
                };
                foreach (var name in names)
                {
                    var shader = Shader.Find(name);
                    if (shader != null)
                    {
                        _cached = shader;
                        Plugin.LogVerbose("Parry VFX shader: " + name);
                        return _cached;
                    }
                }
                return null;
            }
        }

        public static Material Make(Color color)
        {
            var shader = Unlit;
            if (shader == null)
                return null;
            var mat = new Material(shader) { color = color };
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            return mat;
        }
    }
}
