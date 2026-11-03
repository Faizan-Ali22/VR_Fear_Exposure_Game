using UnityEngine;

namespace FearExposure.Level1
{
    /// <summary>Small helpers so scripts can tint materials without caring which URP shader is used.</summary>
    internal static class MaterialUtil
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        /// <summary>Private copy of a material, so changing its colour never edits the asset.</summary>
        public static Material Clone(Material source)
        {
            if (source == null) return null;
            Material m = new Material(source);
            m.name = source.name + " (runtime)";
            return m;
        }

        public static void SetColor(Material m, Color c)
        {
            if (m == null) return;
            if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, c);
            if (m.HasProperty(ColorId)) m.SetColor(ColorId, c);
        }

        public static void SetTexture(Material m, Texture t)
        {
            if (m == null) return;
            if (m.HasProperty(BaseMapId)) m.SetTexture(BaseMapId, t);
            else if (m.HasProperty(MainTexId)) m.SetTexture(MainTexId, t);
        }
    }
}
