using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BreachPoint
{
    /// <summary>
    /// Art pass hooks. Everything here is optional: if an asset is missing, the game falls back
    /// to the code-built placeholder, so art can be dropped in piece by piece.
    ///
    ///   Resources/Art/Textures/&lt;Name&gt;_diff.jpg (+ _nor.jpg)  → Textured("Name", …)
    ///   Resources/Art/Models/&lt;Slot&gt;.fbx                       → Model("Slot", parent)
    ///   Resources/Art/Sky/RooftopNight.hdr                      → reflections (Environment())
    /// </summary>
    public static class ArtKit
    {
        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

        /// <summary>Lit material using a texture set; falls back to a flat colour if the textures are missing.</summary>
        public static Material Textured(string name, Color tint, float tiling, float smooth = 0.4f, float metal = 0f, float normalStrength = 1f)
        {
            string key = $"{name}|{tint}|{tiling}|{smooth}|{metal}|{normalStrength}";
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            var diff = Resources.Load<Texture2D>($"Art/Textures/{name}_diff");
            if (diff == null) return Cache[key] = Mats.Lit(tint, smooth, metal);

            m = new Material(Mats.Lit(Color.white, smooth, metal)) { name = name };
            m.SetTexture("_BaseMap", diff);
            m.SetColor("_BaseColor", tint);
            m.SetTextureScale("_BaseMap", new Vector2(tiling, tiling));
            var nor = Resources.Load<Texture2D>($"Art/Textures/{name}_nor");
            if (nor != null)
            {
                m.SetTexture("_BumpMap", nor);
                m.SetFloat("_BumpScale", normalStrength);
                m.EnableKeyword("_NORMALMAP");
            }
            return Cache[key] = m;
        }

        /// <summary>Unique textured material with its own tiling (for big boxes whose faces would otherwise stretch).</summary>
        public static Material TexturedSized(string name, Color tint, Vector2 tiling, float smooth = 0.4f, float metal = 0f)
        {
            var m = new Material(Textured(name, tint, 1f, smooth, metal));
            m.SetTextureScale("_BaseMap", tiling);
            return m;
        }

        /// <summary>
        /// Scales and centres a model inside a box of <paramref name="size"/> (parent-local units).
        /// Most kits model "forward" as +Z; our vehicles drive along +X, so zForward rotates it.
        /// </summary>
        public static void Fit(GameObject go, Vector3 size, Vector3 center, bool zForward = true)
        {
            var t = go.transform;
            if (zForward) t.localRotation = Quaternion.Euler(0, 90f, 0);
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;
            Bounds Measure()
            {
                var b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
                return b;
            }
            float ps = t.parent != null ? t.parent.lossyScale.x : 1f;
            var ext = Measure().size / ps;
            float k = Mathf.Min(size.x / Mathf.Max(ext.x, 1e-4f), size.y / Mathf.Max(ext.y, 1e-4f), size.z / Mathf.Max(ext.z, 1e-4f));
            t.localScale *= k;
            var b2 = Measure();
            var localCenter = t.parent != null ? t.parent.InverseTransformPoint(b2.center) : b2.center;
            t.localPosition += center - localCenter;
        }

        /// <summary>
        /// Instantiates Resources/Art/Models/&lt;slot&gt; under parent, or returns null if no model is installed.
        /// Colliders are stripped; shadows on.
        /// </summary>
        public static GameObject Model(string slot, Transform parent)
        {
            var prefab = Resources.Load<GameObject>("Art/Models/" + slot);
            if (prefab == null) return null;
            var go = Object.Instantiate(prefab, parent, false);
            go.name = slot;
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.On;
            return go;
        }

        public static bool HasModel(string slot) => Resources.Load<GameObject>("Art/Models/" + slot) != null;

        /// <summary>Night-city reflections from the HDRI (makes metal and wet surfaces read as premium).</summary>
        public static void Environment()
        {
            var sky = Resources.Load<Cubemap>("Art/Sky/RooftopNight");
            if (sky == null) return;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = sky;
            RenderSettings.reflectionIntensity = 0.55f;
        }
    }
}
