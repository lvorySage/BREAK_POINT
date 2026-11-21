using UnityEngine;
using UnityEngine.Rendering;

namespace BreachPoint
{
    /// <summary>Track, fork lanes with painted links, props and lure visuals for Phish Race.</summary>
    public sealed partial class PhishRace
    {
        static readonly Color LinkCyan = new Color(0.55f, 0.95f, 1f);
        static readonly Color Magenta = new Color(1f, 0.2f, 0.7f);

        readonly TextMesh[,] _laneText = new TextMesh[2, 2];      // [fork, outer?1:0]
        readonly Material[,] _laneMat = new Material[2, 2];
        readonly float[,] _laneFlash = new float[2, 2];
        readonly Color[,] _laneFlashColor = new Color[2, 2];
        readonly TextMesh[] _forkHeader = new TextMesh[2];

        void BuildTrack()
        {
            var root = transform;
            var slab = ArtKit.TexturedSized("RoofConcrete", new Color(0.3f, 0.32f, 0.38f), new Vector2(16f, 10f), 0.6f, 0.1f);
            Shapes.Box(root, "Roof", new Vector3(0, -0.5f, 0), new Vector3(64, 1, 40), slab);

            // Asphalt ribbon (generated mesh), slightly wet-looking.
            var asphalt = ArtKit.Textured("RoofConcrete", new Color(0.16f, 0.17f, 0.21f), 1f, 0.78f, 0.15f);
            var road = new GameObject("Track");
            road.transform.SetParent(root, false);
            road.transform.localPosition = new Vector3(0, 0.02f, 0);
            road.AddComponent<MeshFilter>().sharedMesh = TrackMesh(-Width / 2f, Width / 2f, 160);
            var mr = road.AddComponent<MeshRenderer>();
            mr.sharedMaterial = asphalt;
            mr.shadowCastingMode = ShadowCastingMode.Off;

            // Neon kerbs and a dashed centre line.
            Loop(root, "KerbInner", -Width / 2f, 0.16f, Mats.Glow(LinkCyan, 1.6f));
            Loop(root, "KerbOuter", Width / 2f, 0.16f, Mats.Glow(Magenta, 1.8f));
            var dash = Mats.Glow(new Color(0.85f, 0.9f, 1f), 0.9f);
            for (float s = 0; s < Perimeter; s += 3f)
            {
                var p = At(s);
                if (Mathf.Abs(p.x) < ForkHalf + 0.5f) continue; // the lane dividers are the centre line inside the forks
                var d = At(s + 1.2f) - p;
                var seg = Shapes.Box(root, "Dash", (p + At(s + 1.2f)) / 2f + Vector3.up * 0.04f, new Vector3(0.12f, 0.02f, d.magnitude), dash, false);
                seg.transform.localRotation = Quaternion.LookRotation(d);
            }

            // Start / finish line: chequered strip across the track at s = 0 (the right-hand apex).
            var white = Mats.Lit(new Color(0.9f, 0.9f, 0.92f), 0.4f);
            var black = Mats.Lit(new Color(0.04f, 0.04f, 0.05f), 0.4f);
            for (int k = 0; k < 10; k++)
            for (int row = 0; row < 2; row++)
            {
                float x = L + R - Width / 2f + 0.4f + k * 0.8f;
                Shapes.Box(root, "Checker", new Vector3(x, 0.04f, -0.4f + row * 0.8f), new Vector3(0.8f, 0.02f, 0.8f), (k + row) % 2 == 0 ? white : black, false);
            }
            var gantry = ArtKit.Model("RaceGantry", root);
            if (gantry != null) ArtKit.Fit(gantry, new Vector3(Width + 2.5f, 5f, 2f), new Vector3(L + R, 2.5f, 0), zForward: false);
            else
            {
                var metal = ArtKit.Textured("MetalPlate", new Color(0.3f, 0.3f, 0.36f), 1f, 0.6f, 0.5f);
                foreach (var x in new[] { L + R - Width / 2f - 0.6f, L + R + Width / 2f + 0.6f })
                    Shapes.Box(root, "GantryLeg", new Vector3(x, 2.2f, 0), new Vector3(0.4f, 4.4f, 0.4f), metal);
                Shapes.Box(root, "GantryBeam", new Vector3(L + R, 4.4f, 0), new Vector3(Width + 1.6f, 0.6f, 0.5f), metal);
            }
            Shapes.Text3D(root, "START / FINISH", new Vector3(L + R, 6.2f, 0), 0.075f, Palette.Gold, true, Fonts.Title);

            for (int f = 0; f < 2; f++) BuildFork(root, f);
            BuildInfield(root);
            BuildProps(root);

            Shapes.PointLight(root, new Vector3(-14, 7, 0), new Color(1f, 0.4f, 0.6f), 24, 2f);
            Shapes.PointLight(root, new Vector3(14, 7, 0), new Color(0.4f, 0.75f, 1f), 24, 2f);
            Shapes.PointLight(root, new Vector3(0, 8, 8), new Color(0.85f, 0.9f, 1f), 18, 1.4f);
            Shapes.PointLight(root, new Vector3(0, 8, -8), new Color(0.85f, 0.9f, 1f), 18, 1.4f);
        }

        static Mesh TrackMesh(float inner, float outer, int n)
        {
            var verts = new Vector3[(n + 1) * 2];
            var uvs = new Vector2[verts.Length];
            var tris = new int[n * 6];
            for (int k = 0; k <= n; k++)
            {
                float s = Perimeter * k / n;
                verts[k * 2] = At(s, inner);
                verts[k * 2 + 1] = At(s, outer);
                uvs[k * 2] = new Vector2(0, s / 4f);
                uvs[k * 2 + 1] = new Vector2((outer - inner) / 4f, s / 4f);
                if (k == n) break;
                int t = k * 6, v = k * 2;
                // Counter-clockwise travel with the inner edge on the left: wind so the face points up.
                tris[t] = v; tris[t + 1] = v + 2; tris[t + 2] = v + 1;
                tris[t + 3] = v + 1; tris[t + 4] = v + 2; tris[t + 5] = v + 3;
            }
            var m = new Mesh { name = "TrackMesh", vertices = verts, uv = uvs, triangles = tris };
            m.RecalculateNormals();
            // Guarantee upward normals whichever way the winding came out.
            if (m.normals.Length > 0 && m.normals[0].y < 0)
            {
                for (int t = 0; t < tris.Length; t += 3) (tris[t + 1], tris[t + 2]) = (tris[t + 2], tris[t + 1]);
                m.triangles = tris;
                m.RecalculateNormals();
            }
            m.RecalculateBounds();
            return m;
        }

        static void Loop(Transform root, string name, float offset, float width, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.widthMultiplier = width;
            lr.sharedMaterial = mat;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.alignment = LineAlignment.TransformZ;
            go.transform.localRotation = Quaternion.Euler(90f, 0, 0); // lie flat on the road
            const int n = 120;
            lr.positionCount = n;
            for (int k = 0; k < n; k++)
            {
                var p = At(Perimeter * k / n, offset);
                lr.SetPosition(k, new Vector3(p.x, p.z, -0.06f)); // local XY = world XZ after the rotation
            }
        }

        static float LaneZ(int fork, bool outer) => (fork == 0 ? 1f : -1f) * (R + (outer ? 2f : -2f));

        void BuildFork(Transform root, int fork)
        {
            float sz = fork == 0 ? 1f : -1f;
            var metal = ArtKit.Textured("MetalPlate", new Color(0.35f, 0.35f, 0.4f), 1f, 0.6f, 0.5f);
            // Lane divider wall with a neon top and glowing noses.
            Shapes.Box(root, "Divider", new Vector3(0, 0.35f, sz * R), new Vector3(ForkHalf * 2f, 0.7f, 0.4f), metal);
            Shapes.Box(root, "DividerNeon", new Vector3(0, 0.73f, sz * R), new Vector3(ForkHalf * 2f, 0.06f, 0.42f), Mats.Glow(Palette.Gold, 1.6f), false);
            foreach (var ex in new[] { -ForkHalf, ForkHalf })
            {
                var cone = ArtKit.Model("RacePylon", root);
                if (cone != null) ArtKit.Fit(cone, new Vector3(0.7f, 1f, 0.7f), new Vector3(ex, 0.5f, sz * R), zForward: false);
                else Shapes.Prim(PrimitiveType.Cylinder, root, "Nose", new Vector3(ex, 0.5f, sz * R), new Vector3(0.6f, 0.5f, 0.6f), Mats.Glow(Palette.Gold, 2f), false);
            }

            for (int o = 0; o < 2; o++)
            {
                bool outer = o == 1;
                float z = LaneZ(fork, outer);
                _laneMat[fork, o] = Mats.GlowInstance(LinkCyan, 0.25f);
                Shapes.Box(root, "LanePlate", new Vector3(0, 0.035f, z), new Vector3(ForkHalf * 2f - 0.6f, 0.02f, 3.0f), _laneMat[fork, o], false);
                var tm = Shapes.Text3D(root, "", new Vector3(0, 0.08f, z), 0.2f, Color.white, false, Fonts.Mono);
                tm.transform.localRotation = Quaternion.Euler(90f, 0, 0);
                _laneText[fork, o] = tm;
            }
            // Header sign beyond the outer lane (far side for the top fork, near side for the bottom).
            float hz = sz * (R + Width / 2f + 1.6f);
            var header = Shapes.Text3D(root, "WHICH LINK IS REAL?", new Vector3(0, 1.2f, hz), 0.085f, Palette.Gold, true, Fonts.Title);
            _forkHeader[fork] = header;
            var arrow = fork == 0 ? "◀◀  pick a lane before the divider" : "pick a lane before the divider  ▶▶";
            if (fork == 1) Shapes.Text3D(root, arrow, new Vector3(0, 0.3f, hz + sz * 1.6f), 0.05f, Palette.Muted);
        }

        void ShowForkLinks(int fork)
        {
            if (_laneText[fork, 0] == null) return;
            var pair = ForkLinks(fork);
            for (int o = 0; o < 2; o++)
            {
                bool real = (o == 1) == _realOuter[fork];
                var tm = _laneText[fork, o];
                tm.text = real ? pair.Real : pair.Fake;
                tm.color = Color.white; // identical styling: the only clue is the text itself
                tm.characterSize = 0.1f; // Text3D renders at fontSize 128
                // Fit long links inside the lane.
                float chars = tm.text.Length;
                float width = chars * 0.1f * 128f * 0.1f * 0.52f;
                float maxW = ForkHalf * 2f + 4f; // long expert links may run a little past the lane plate
                if (width > maxW) tm.characterSize *= maxW / width;
                _laneFlash[fork, o] = 0.5f;
                _laneFlashColor[fork, o] = LinkCyan;
            }
        }

        void FlashLane(int fork, bool outer, bool safe)
        {
            int o = outer ? 1 : 0;
            _laneFlash[fork, o] = 1.2f;
            _laneFlashColor[fork, o] = safe ? Palette.Good : Palette.Bad;
        }

        void BuildInfield(Transform root)
        {
            // Holographic title in the infield.
            var t = Shapes.Text3D(root, "PHISH RACE", new Vector3(0, 0.1f, 0.6f), 0.2f, Palette.A(LinkCyan, 0.85f), false, Fonts.Title);
            t.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            var t2 = Shapes.Text3D(root, "Read the link before you click", new Vector3(0, 0.1f, -1.4f), 0.07f, Palette.Muted, false);
            t2.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            var infield = Mats.Lit(new Color(0.06f, 0.08f, 0.12f), 0.85f, 0.2f);
            var island = new GameObject("Infield");
            island.transform.SetParent(root, false);
            island.transform.localPosition = new Vector3(0, 0.015f, 0);
            island.AddComponent<MeshFilter>().sharedMesh = TrackMesh(-R - 0.01f, -Width / 2f, 160);
            island.AddComponent<MeshRenderer>().sharedMaterial = infield;
        }

        void BuildProps(Transform root)
        {
            // Grandstands behind the far straight, banner towers at the bends.
            for (int k = -1; k <= 1; k++)
            {
                var stand = ArtKit.Model("RaceStand", root);
                if (stand != null)
                {
                    stand.transform.localRotation = Quaternion.Euler(0, 180f, 0) * stand.transform.localRotation;
                    ArtKit.Fit(stand, new Vector3(7.5f, 4f, 4f), new Vector3(k * 8f + 0f, 2f, R + Width / 2f + 6.5f), zForward: false);
                }
            }
            foreach (var p in new[] { new Vector3(-L - R - 6f, 0, -9f), new Vector3(L + R + 6f, 0, 9f), new Vector3(-L - R - 6f, 0, 9f), new Vector3(L + R + 6f, 0, -9f) })
            {
                var banner = ArtKit.Model("RaceBanner", root);
                if (banner != null) ArtKit.Fit(banner, new Vector3(2f, 6f, 2f), p + Vector3.up * 3f, zForward: false);
            }
            var ac = ArtKit.Model("RoofAC", root);
            if (ac != null) ArtKit.Fit(ac, new Vector3(3f, 2f, 3f), new Vector3(-26f, 1f, -15f), zForward: false);
            var ant = ArtKit.Model("RoofAntenna", root);
            if (ant != null) ArtKit.Fit(ant, new Vector3(1.5f, 8f, 1.5f), new Vector3(27f, 4f, -15f), zForward: false);
        }

        GameObject BuildLure(Vector3 pos, string text)
        {
            var g = new GameObject("Lure");
            g.transform.SetParent(transform, false);
            g.transform.localPosition = pos;
            var glow = Mats.GlowInstance(Magenta, 2.2f);
            var env = Shapes.Box(g.transform, "Envelope", new Vector3(0, 0.9f, 0), new Vector3(1.3f, 0.85f, 0.15f), glow, false);
            env.AddComponent<Spin>();
            Shapes.Ring(g.transform, "Ring", LureRadius, 0.08f, glow).transform.localPosition = new Vector3(0, 0.06f, 0);
            Shapes.Text3D(g.transform, text, new Vector3(0, 2.2f, 0), 0.075f, new Color(1f, 0.5f, 0.85f));
            Shapes.PointLight(g.transform, new Vector3(0, 1f, 0), Magenta, 4f, 2.5f);
            return g;
        }

        void UpdateVisuals(float dt)
        {
            if (!_built) return;
            for (int f = 0; f < 2; f++)
            for (int o = 0; o < 2; o++)
            {
                if (_laneMat[f, o] == null) continue;
                _laneFlash[f, o] = Mathf.Max(0, _laneFlash[f, o] - dt);
                float k = Mathf.Clamp01(_laneFlash[f, o]);
                var c = Color.Lerp(LinkCyan, _laneFlashColor[f, o], k);
                Mats.SetGlow(_laneMat[f, o], c, 0.22f + 1.4f * k);
            }
            for (int i = 0; i < 2; i++)
            {
                if (_veh[i] == null) continue;
                // Stalled ships wobble; surging ships glow.
                if (_veh[i].Body != null && _stall[i] > 0)
                    _veh[i].Body.rotation *= Quaternion.Euler(Mathf.Sin(Time.time * 30f) * 6f, Mathf.Sin(Time.time * 23f) * 10f, 0);
            }
        }
    }

    /// <summary>Slow spin for floating pickups.</summary>
    public sealed class Spin : MonoBehaviour
    {
        public float Speed = 120f;
        void Update() => transform.Rotate(0, Speed * Time.deltaTime, 0, Space.World);
    }
}
