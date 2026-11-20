using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BreachPoint
{
    /// <summary>Material factory. Base materials live in Resources so they are included in builds.</summary>
    public static class Mats
    {
        static Material _lit, _unlit;
        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

        static Material BaseLit
        {
            get
            {
                if (_lit == null) _lit = Resources.Load<Material>("Materials/BP_Lit");
                if (_lit == null) _lit = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                return _lit;
            }
        }

        static Material BaseUnlit
        {
            get
            {
                if (_unlit == null) _unlit = Resources.Load<Material>("Materials/BP_Unlit");
                if (_unlit == null) _unlit = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                return _unlit;
            }
        }

        /// <summary>Lit surface, optional emission (HDR colour, &gt;1 blooms).</summary>
        public static Material Lit(Color c, float smooth = 0.5f, float metal = 0f, Color? emission = null)
        {
            string key = $"L{c}{smooth}{metal}{emission}";
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(BaseLit) { name = "Lit" };
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", metal);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            return Cache[key] = m;
        }

        /// <summary>Unlit glowing colour. Intensity &gt; 1 feeds the bloom.</summary>
        public static Material Glow(Color c, float intensity = 3f)
        {
            string key = $"G{c}{intensity}";
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(BaseUnlit) { name = "Glow" };
            m.SetColor("_BaseColor", Palette.Hdr(c, intensity));
            return Cache[key] = m;
        }

        /// <summary>Unique (uncached) glow material for things that change colour at runtime.</summary>
        public static Material GlowInstance(Color c, float intensity = 3f)
        {
            var m = new Material(BaseUnlit) { name = "GlowInstance" };
            m.SetColor("_BaseColor", Palette.Hdr(c, intensity));
            return m;
        }

        public static void SetGlow(Material m, Color c, float intensity) => m.SetColor("_BaseColor", Palette.Hdr(c, intensity));
    }

    public static class Shapes
    {
        public static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 localPos, Vector3 scale, Material m, bool shadows = true)
        {
            var g = GameObject.CreatePrimitive(type);
            g.name = name;
            var col = g.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            g.transform.localScale = scale;
            var r = g.GetComponent<MeshRenderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return g;
        }

        public static GameObject Box(Transform p, string n, Vector3 pos, Vector3 scale, Material m, bool shadows = true)
            => Prim(PrimitiveType.Cube, p, n, pos, scale, m, shadows);

        public static GameObject Ball(Transform p, string n, Vector3 pos, float size, Material m)
            => Prim(PrimitiveType.Sphere, p, n, pos, Vector3.one * size, m, false);

        public static LineRenderer Ring(Transform parent, string name, float radius, float width, Material m, int segments = 48)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = segments;
            lr.widthMultiplier = width;
            lr.sharedMaterial = m;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
            return lr;
        }

        public static Light PointLight(Transform parent, Vector3 pos, Color c, float range, float intensity)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = c;
            l.range = range;
            l.intensity = intensity;
            l.shadows = LightShadows.None;
            return l;
        }

        public static TextMesh Text3D(Transform parent, string text, Vector3 localPos, float size, Color c, bool billboard = true, Font font = null)
        {
            var go = new GameObject("Text3D");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<TextMesh>();
            tm.font = font != null ? font : Fonts.Bold;
            tm.fontSize = 64;
            tm.characterSize = size;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.richText = true;
            tm.text = text;
            tm.color = c;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = tm.font.material;
            r.shadowCastingMode = ShadowCastingMode.Off;
            if (billboard) go.AddComponent<Billboard>();
            return tm;
        }
    }

    public sealed class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
        }
    }

    /// <summary>Floating world text that rises and fades ("BLOCKED!", "-8% DATA").</summary>
    public sealed class Popup : MonoBehaviour
    {
        TextMesh _tm;
        Color _c;
        float _t;
        const float Life = 1.2f;

        public static void Show(Vector3 worldPos, string text, Color c, float size = 0.13f)
        {
            // Slight random offset so simultaneous popups don't stack on top of each other.
            worldPos += new Vector3(Random.Range(-0.9f, 0.9f), Random.Range(0f, 0.6f), Random.Range(-0.4f, 0.4f));
            var tm = Shapes.Text3D(null, text, worldPos, size, c);
            tm.gameObject.name = "Popup";
            var p = tm.gameObject.AddComponent<Popup>();
            p._tm = tm;
            p._c = c;
        }

        void Update()
        {
            _t += Time.deltaTime;
            transform.position += Vector3.up * (2.2f * Time.deltaTime);
            float k = Mathf.Clamp01(_t / Life);
            _tm.color = Palette.A(_c, 1f - k * k);
            transform.localScale = Vector3.one * (1f + 0.25f * Mathf.Sin(Mathf.Min(_t * 12f, Mathf.PI)));
            if (_t >= Life) Destroy(gameObject);
        }
    }

    /// <summary>Camera, lighting, fog and post-processing shared by every screen.</summary>
    public static class Stage
    {
        public static Camera CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.015f, 0.02f, 0.045f);
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 400f;
            go.AddComponent<AudioListener>();
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            return cam;
        }

        public static void Lighting()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.26f, 0.27f, 0.4f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.03f, 0.03f, 0.08f);
            RenderSettings.fogDensity = 0.007f;

            var sun = new GameObject("Moonlight").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(0.55f, 0.6f, 0.95f);
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
        }

        public static Volume PostFx()
        {
            var go = new GameObject("PostFX");
            var vol = go.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 10;
            var p = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = p.Add<Bloom>(true);
            bloom.threshold.Override(1.05f);
            bloom.intensity.Override(0.85f);
            bloom.scatter.Override(0.72f);
            var tone = p.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral); // keeps neon hues saturated
            var vig = p.Add<Vignette>(true);
            vig.intensity.Override(0.32f);
            vig.smoothness.Override(0.5f);
            var ca = p.Add<ChromaticAberration>(true);
            ca.intensity.Override(0.06f);
            var color = p.Add<ColorAdjustments>(true);
            color.contrast.Override(12f);
            color.saturation.Override(10f);
            vol.profile = p;
            return vol;
        }
    }
}
