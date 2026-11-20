using UnityEngine;
using UnityEngine.UI;

namespace BreachPoint
{
    /// <summary>Small code-only uGUI kit (1920×1080 reference layout).</summary>
    public static class Ui
    {
        public static Canvas Canvas(string name, int order)
        {
            var go = new GameObject(name);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            var s = go.AddComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920, 1080);
            s.matchWidthOrHeight = 0.5f;
            return c;
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 oMin = default, Vector2 oMax = default)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = oMin;
            rt.offsetMax = oMax;
            return rt;
        }

        public static RectTransform Full(Transform parent, string name = "Screen") => Rect(parent, name, Vector2.zero, Vector2.one);

        public static Image Panel(Transform parent, Color c, Vector2 aMin, Vector2 aMax, Vector2 oMin = default, Vector2 oMax = default, string name = "Panel")
        {
            var rt = Rect(parent, name, aMin, aMax, oMin, oMax);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>A panel with a coloured top edge, centred on a pivot point.</summary>
        public static Image Box(Transform parent, Vector2 anchor, Vector2 size, Color edge, string name = "Box")
        {
            var img = Panel(parent, Palette.Panel, anchor, anchor, -size / 2f, size / 2f, name);
            Panel(img.transform, edge, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -5), Vector2.zero, "Edge");
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color c, TextAnchor align,
            Vector2 aMin, Vector2 aMax, Vector2 oMin = default, Vector2 oMax = default, Font font = null, bool shadow = true)
        {
            var rt = Rect(parent, "Label", aMin, aMax, oMin, oMax);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font != null ? font : Fonts.Bold;
            t.fontSize = size;
            t.color = c;
            t.alignment = align;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.text = text;
            if (shadow)
            {
                var sh = rt.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0, 0, 0, 0.75f);
                sh.effectDistance = new Vector2(2, -2);
            }
            return t;
        }

        /// <summary>Label stretched over its parent.</summary>
        public static Text Fill(Transform parent, string text, int size, Color c, TextAnchor align = TextAnchor.MiddleCenter, Font font = null)
            => Label(parent, text, size, c, align, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, font);

        /// <summary>Horizontal bar. Set the returned fill's anchorMax.x (0..1) to update.</summary>
        public static RectTransform Bar(Transform parent, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, Color fill)
        {
            var bg = Panel(parent, new Color(0, 0, 0, 0.55f), aMin, aMax, oMin, oMax, "Bar");
            var f = Panel(bg.transform, fill, Vector2.zero, new Vector2(0, 1), Vector2.zero, Vector2.zero, "Fill");
            return f.rectTransform;
        }

        public static void SetBar(RectTransform fill, float v) => fill.anchorMax = new Vector2(Mathf.Clamp01(v), 1f);
    }
}
